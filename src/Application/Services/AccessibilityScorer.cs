namespace WcagAnalyzer.Application.Services;

public static class AccessibilityScorer
{
    private const int TotalRules = 111;

    public static int Calculate(IEnumerable<(string RuleId, string Impact)> results)
    {
        var uniqueRules = results.GroupBy(r => r.RuleId).ToList();
        int uniqueViolating = uniqueRules.Count;
        double passRate = (double)Math.Max(0, TotalRules - uniqueViolating) / TotalRules;

        var uniqueByImpact = uniqueRules
            .GroupBy(g => g.First().Impact)
            .ToDictionary(g => g.Key, g => g.Count());

        int critical = uniqueByImpact.GetValueOrDefault("critical", 0);
        int serious  = uniqueByImpact.GetValueOrDefault("serious",  0);
        int moderate = uniqueByImpact.GetValueOrDefault("moderate", 0);
        int minor    = uniqueByImpact.GetValueOrDefault("minor",    0);

        double logBase = Math.Log2(TotalRules + 1);
        double penalty =
            15 * Math.Log2(1 + critical)  / logBase +
             8 * Math.Log2(1 + serious)   / logBase +
             4 * Math.Log2(1 + moderate)  / logBase +
             1 * Math.Log2(1 + minor)     / logBase;

        return (int)Math.Round(Math.Max(0, passRate * 100 - penalty));
    }
}
