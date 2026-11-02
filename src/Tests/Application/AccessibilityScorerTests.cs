using FluentAssertions;
using WcagAnalyzer.Application.Services;

namespace WcagAnalyzer.Tests.Application;

public class AccessibilityScorerTests
{
    private static (string RuleId, string Impact) Result(string ruleId, string impact) => (ruleId, impact);

    [Fact]
    public void NoResults_ShouldReturnPerfectScore()
    {
        var score = AccessibilityScorer.Calculate([]);

        score.Should().Be(100);
    }

    [Fact]
    public void OneCriticalRule_ShouldMatchExactComputedScore()
    {
        // passRate=110/111≈99.10%, penalty=15*log2(2)/log2(112)≈2.20 → 99.10-2.20=96.90 → rounds to 97
        var score = AccessibilityScorer.Calculate([Result("color-contrast", "critical")]);

        score.Should().Be(97);
    }

    [Fact]
    public void MixedImpactsScenario_ShouldMatchExactComputedScore()
    {
        // 2 critical, 1 serious, 1 moderate, 1 minor unique rules → 90
        var results = new List<(string RuleId, string Impact)>
        {
            Result("a", "critical"),
            Result("b", "critical"),
            Result("c", "serious"),
            Result("d", "moderate"),
            Result("e", "minor"),
        };

        var score = AccessibilityScorer.Calculate(results);

        score.Should().Be(90);
    }

    [Fact]
    public void DuplicateRuleId_ShouldBeCountedOnceTowardScore()
    {
        var single = AccessibilityScorer.Calculate([Result("color-contrast", "critical")]);

        var repeated = AccessibilityScorer.Calculate(
            Enumerable.Range(0, 10).Select(_ => Result("color-contrast", "critical")));

        repeated.Should().Be(single);
    }

    [Fact]
    public void DuplicateRuleIdWithDifferingImpact_ShouldUseFirstOccurrenceImpact()
    {
        // Calculate groups by RuleId and reads .First().Impact for the bucket — if the same
        // rule somehow appears twice with different impact strings, only the first counts.
        var firstCriticalThenMinor = AccessibilityScorer.Calculate(
        [
            Result("flaky-rule", "critical"),
            Result("flaky-rule", "minor"),
        ]);

        var criticalOnly = AccessibilityScorer.Calculate([Result("flaky-rule", "critical")]);

        firstCriticalThenMinor.Should().Be(criticalOnly);
    }

    [Fact]
    public void HigherImpact_ShouldDeductMoreThanLowerImpact_ForSameRuleCount()
    {
        (string, string) Rule(int i, string impact) => ($"rule-{i}", impact);

        var critical = AccessibilityScorer.Calculate(Enumerable.Range(0, 5).Select(i => Rule(i, "critical")));
        var serious  = AccessibilityScorer.Calculate(Enumerable.Range(0, 5).Select(i => Rule(i, "serious")));
        var moderate = AccessibilityScorer.Calculate(Enumerable.Range(0, 5).Select(i => Rule(i, "moderate")));
        var minor    = AccessibilityScorer.Calculate(Enumerable.Range(0, 5).Select(i => Rule(i, "minor")));

        critical.Should().BeLessThan(serious);
        serious.Should().BeLessThan(moderate);
        moderate.Should().BeLessThan(minor);
    }

    [Fact]
    public void UnrecognizedImpact_ShouldOnlyAffectPassRateNotPenalty()
    {
        // "unknown" isn't critical/serious/moderate/minor, so it contributes 0 to the
        // weighted penalty — only the pass-rate term (one fewer passing rule) applies.
        var unknownImpact = AccessibilityScorer.Calculate([Result("some-rule", "unknown")]);

        unknownImpact.Should().Be(99);
    }

    [Fact]
    public void UniqueViolatingRulesEqualToTotalRules_PassRateShouldBottomOutAtZero()
    {
        // Exactly 111 unique critical rules — passRate hits exactly 0, and the critical
        // penalty alone (15) outweighs it, so the final score clamps to 0.
        var results = Enumerable.Range(0, 111).Select(i => Result($"rule-{i}", "critical"));

        var score = AccessibilityScorer.Calculate(results);

        score.Should().Be(0);
    }

    [Fact]
    public void UniqueViolatingRulesExceedingTotalRules_ShouldNotGoNegative()
    {
        // Math.Max(0, TotalRules - uniqueViolating) must clamp rather than go negative
        // and invert the pass-rate term when there are more violating rules than exist.
        var results = Enumerable.Range(0, 200).Select(i => Result($"rule-{i}", "critical"));

        var score = AccessibilityScorer.Calculate(results);

        score.Should().Be(0);
    }

    [Fact]
    public void ScoreShouldNeverExceedOneHundred()
    {
        var score = AccessibilityScorer.Calculate([]);

        score.Should().BeLessThanOrEqualTo(100);
    }
}
