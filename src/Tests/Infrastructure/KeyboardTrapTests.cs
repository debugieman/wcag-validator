using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class KeyboardTrapTests
{
    [Fact]
    public void NotTrapped_ShouldReturnNull()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeKeyboardTrap(trapped: false, elementIdentifier: "");

        result.Should().BeNull();
    }

    [Fact]
    public void Trapped_ShouldReturnViolation()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeKeyboardTrap(trapped: true, elementIdentifier: "button#close");

        result.Should().NotBeNull();
        result!.RuleId.Should().Be("keyboard-trap");
        result.Impact.Should().Be("critical");
    }

    [Fact]
    public void Trapped_ViolationShouldContainElementIdentifier()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeKeyboardTrap(trapped: true, elementIdentifier: "div.modal");

        result!.Description.Should().Contain("div.modal");
    }

    [Fact]
    public void ViolationDescription_ShouldMentionWCAG212()
    {
        var result = PlaywrightAccessibilityAnalyzer.AnalyzeKeyboardTrap(trapped: true, elementIdentifier: "input");

        result!.Description.Should().Contain("WCAG 2.1.2");
    }
}
