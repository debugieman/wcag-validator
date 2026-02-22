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
            var violations = ParseViolations(resultsJson);

            var headingViolations = await CheckHeadingHierarchyAsync(page);
            violations.AddRange(headingViolations);

            var langViolation = await CheckHtmlLangAsync(page);
            if (langViolation is not null)
                violations.Add(langViolation);

            return violations;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    internal static List<AccessibilityViolation> ParseViolations(JsonElement results)
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

    private static async Task<List<AccessibilityViolation>> CheckHeadingHierarchyAsync(IPage page)
    {
        var headingsJson = await page.EvaluateAsync<JsonElement>("""
            () => Array.from(document.querySelectorAll('h1,h2,h3,h4,h5,h6')).map(h => ({
                level: parseInt(h.tagName[1]),
                html: h.outerHTML
            }))
        """);

        var headings = new List<HeadingInfo>();
        foreach (var h in headingsJson.EnumerateArray())
        {
            headings.Add(new HeadingInfo(
                h.GetProperty("level").GetInt32(),
                h.GetProperty("html").GetString() ?? ""));
        }

        return AnalyzeHeadingHierarchy(headings);
    }

    internal static List<AccessibilityViolation> AnalyzeHeadingHierarchy(List<HeadingInfo> headings)
    {
        var violations = new List<AccessibilityViolation>();

        if (headings.Count == 0)
            return violations;

        var hasH1 = false;
        var previousLevel = 0;

        // Check if first heading is not h1
        if (headings[0].Level != 1)
        {
            violations.Add(new AccessibilityViolation
            {
                RuleId = "heading-first-not-h1",
                Impact = "moderate",
                Description = "First heading on the page should be an <h1>",
                HtmlElement = headings[0].OuterHtml
            });
        }

        foreach (var heading in headings)
        {
            // Check for multiple h1
            if (heading.Level == 1)
            {
                if (hasH1)
                {
                    violations.Add(new AccessibilityViolation
                    {
                        RuleId = "heading-multiple-h1",
                        Impact = "moderate",
                        Description = "Page should not contain more than one <h1>",
                        HtmlElement = heading.OuterHtml
                    });
                }
                hasH1 = true;
            }

            // Check for skipped levels
            if (previousLevel > 0 && heading.Level > previousLevel + 1)
            {
                violations.Add(new AccessibilityViolation
                {
                    RuleId = "heading-level-skipped",
                    Impact = "moderate",
                    Description = $"Heading level was skipped: expected <h{previousLevel + 1}> or lower, found <h{heading.Level}>",
                    HtmlElement = heading.OuterHtml
                });
            }

            previousLevel = heading.Level;
        }

        // Check for missing h1
        if (!hasH1)
        {
            violations.Add(new AccessibilityViolation
            {
                RuleId = "heading-missing-h1",
                Impact = "moderate",
                Description = "Page should contain a top-level <h1> heading"
            });
        }

        return violations;
    }

    private static async Task<AccessibilityViolation?> CheckHtmlLangAsync(IPage page)
    {
        var lang = await page.EvaluateAsync<string>("() => document.documentElement.getAttribute('lang') || ''");
        if (string.IsNullOrWhiteSpace(lang))
        {
            return new AccessibilityViolation
            {
                RuleId = "html-missing-lang",
                Impact = "serious",
                Description = "The <html> element should have a valid lang attribute"
            };
        }
        return null;
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

internal record HeadingInfo(int Level, string OuterHtml);
