using System.Reflection;
using System.Text.Json;
using Microsoft.Playwright;
using WcagAnalyzer.Application.Models;
using WcagAnalyzer.Application.Services;

namespace WcagAnalyzer.Infrastructure.Services;

public class PlaywrightAccessibilityAnalyzer : IAccessibilityAnalyzer
{
    private readonly PlaywrightBrowserManager _browserManager;
    private static readonly Lazy<string> AxeScript = new(() => LoadAxeScript());

    public PlaywrightAccessibilityAnalyzer(PlaywrightBrowserManager browserManager)
    {
        _browserManager = browserManager;
    }

    public async Task<IReadOnlyList<AccessibilityViolation>> AnalyzeAsync(string url, CancellationToken cancellationToken)
    {
        var browser = await _browserManager.GetBrowserAsync();
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.Load,
                Timeout = 30000
            });

            await page.AddScriptTagAsync(new PageAddScriptTagOptions
            {
                Content = AxeScript.Value
            });

            var resultsJson = await page.EvaluateAsync<JsonElement>("() => axe.run().then(r => r)");

            return ParseViolations(resultsJson);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private static List<AccessibilityViolation> ParseViolations(JsonElement results)
    {
        var violations = new List<AccessibilityViolation>();

        if (!results.TryGetProperty("violations", out var violationsArray))
            return violations;

        foreach (var violation in violationsArray.EnumerateArray())
        {
            var ruleId = violation.GetProperty("id").GetString() ?? "";
            var impact = violation.TryGetProperty("impact", out var impactEl)
                ? impactEl.GetString() ?? ""
                : "";
            var description = violation.TryGetProperty("description", out var descEl)
                ? descEl.GetString() ?? ""
                : "";
            var helpUrl = violation.TryGetProperty("helpUrl", out var helpEl)
                ? helpEl.GetString()
                : null;

            if (violation.TryGetProperty("nodes", out var nodes))
            {
                foreach (var node in nodes.EnumerateArray())
                {
                    var htmlElement = node.TryGetProperty("html", out var htmlEl)
                        ? htmlEl.GetString()
                        : null;

                    violations.Add(new AccessibilityViolation
                    {
                        RuleId = ruleId,
                        Impact = impact,
                        Description = description,
                        HelpUrl = helpUrl,
                        HtmlElement = htmlElement
                    });
                }
            }
            else
            {
                violations.Add(new AccessibilityViolation
                {
                    RuleId = ruleId,
                    Impact = impact,
                    Description = description,
                    HelpUrl = helpUrl
                });
            }
        }

        return violations;
    }

    private static string LoadAxeScript()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith("axe.min.js"));

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
