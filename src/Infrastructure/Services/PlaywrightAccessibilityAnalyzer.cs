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

            var skipNavViolation = await CheckSkipNavigationAsync(page);
            if (skipNavViolation is not null)
                violations.Add(skipNavViolation);

            var formViolations = await CheckFormInputLabelsAsync(page);
            violations.AddRange(formViolations);

            var titleViolation = await CheckDocumentTitleAsync(page);
            if (titleViolation is not null)
                violations.Add(titleViolation);

            var svgViolations = await CheckSvgImagesAsync(page);
            violations.AddRange(svgViolations);

            var emptyLinkViolations = await CheckEmptyLinksAsync(page);
            violations.AddRange(emptyLinkViolations);

            var viewportViolation = await CheckMetaViewportAsync(page);
            if (viewportViolation is not null)
                violations.Add(viewportViolation);

            var emptyButtonViolations = await CheckEmptyButtonsAsync(page);
            violations.AddRange(emptyButtonViolations);

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
        return AnalyzeHtmlLang(lang);
    }

    internal static AccessibilityViolation? AnalyzeHtmlLang(string lang)
    {
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

    private static async Task<AccessibilityViolation?> CheckSkipNavigationAsync(IPage page)
    {
        var linksJson = await page.EvaluateAsync<JsonElement>("""
            () => Array.from(document.querySelectorAll('a[href^="#"]')).map(a => ({
                href: a.getAttribute('href'),
                text: (a.textContent || a.getAttribute('aria-label') || '').trim().toLowerCase()
            }))
        """);

        var links = new List<SkipLinkInfo>();
        foreach (var link in linksJson.EnumerateArray())
        {
            links.Add(new SkipLinkInfo(
                link.GetProperty("href").GetString() ?? "",
                link.GetProperty("text").GetString() ?? ""));
        }

        return AnalyzeSkipNavigation(links);
    }

    internal static AccessibilityViolation? AnalyzeSkipNavigation(List<SkipLinkInfo> links)
    {
        var skipKeywords = new[] { "skip", "jump", "bypass" };

        var hasSkipLink = links.Any(l =>
            skipKeywords.Any(keyword => l.Text.Contains(keyword)));

        if (!hasSkipLink)
        {
            return new AccessibilityViolation
            {
                RuleId = "skip-navigation-missing",
                Impact = "serious",
                Description = "Page should contain a skip navigation link to allow keyboard users to bypass repeated content (WCAG 2.4.1)"
            };
        }

        return null;
    }

    private static async Task<List<AccessibilityViolation>> CheckFormInputLabelsAsync(IPage page)
    {
        var inputsJson = await page.EvaluateAsync<JsonElement>("""
            () => {
                const excluded = ['hidden', 'submit', 'button', 'reset', 'image'];
                return Array.from(document.querySelectorAll('input'))
                    .filter(i => !excluded.includes(i.type))
                    .map(i => ({
                        id: i.id || '',
                        type: i.type || 'text',
                        ariaLabel: i.getAttribute('aria-label') || '',
                        ariaLabelledBy: i.getAttribute('aria-labelledby') || '',
                        hasLabel: i.id ? !!document.querySelector(`label[for="${i.id}"]`) : false,
                        html: i.outerHTML
                    }));
            }
        """);

        var inputs = new List<FormInputInfo>();
        foreach (var input in inputsJson.EnumerateArray())
        {
            inputs.Add(new FormInputInfo(
                input.GetProperty("id").GetString() ?? "",
                input.GetProperty("type").GetString() ?? "text",
                input.GetProperty("ariaLabel").GetString() ?? "",
                input.GetProperty("ariaLabelledBy").GetString() ?? "",
                input.GetProperty("hasLabel").GetBoolean(),
                input.GetProperty("html").GetString() ?? ""
            ));
        }

        return AnalyzeFormInputLabels(inputs);
    }

    internal static List<AccessibilityViolation> AnalyzeFormInputLabels(List<FormInputInfo> inputs)
    {
        var violations = new List<AccessibilityViolation>();

        foreach (var input in inputs)
        {
            var hasAccessibleName = input.HasLabel
                || !string.IsNullOrWhiteSpace(input.AriaLabel)
                || !string.IsNullOrWhiteSpace(input.AriaLabelledBy);

            if (!hasAccessibleName)
            {
                violations.Add(new AccessibilityViolation
                {
                    RuleId = "input-missing-label",
                    Impact = "critical",
                    Description = $"Form input of type '{input.Type}' has no associated label, aria-label, or aria-labelledby (WCAG 1.3.1)",
                    HtmlElement = input.Html
                });
            }
        }

        return violations;
    }

    private static async Task<AccessibilityViolation?> CheckDocumentTitleAsync(IPage page)
    {
        var title = await page.EvaluateAsync<string>("() => document.title || ''");
        return AnalyzeDocumentTitle(new DocumentTitleInfo(title));
    }

    internal static AccessibilityViolation? AnalyzeDocumentTitle(DocumentTitleInfo titleInfo)
    {
        if (string.IsNullOrWhiteSpace(titleInfo.Title))
        {
            return new AccessibilityViolation
            {
                RuleId = "document-title-missing",
                Impact = "serious",
                Description = "Every page should contain a descriptive <title> element (WCAG 2.4.2)"
            };
        }
        return null;
    }

    private static async Task<List<AccessibilityViolation>> CheckSvgImagesAsync(IPage page)
    {
        var svgsJson = await page.EvaluateAsync<JsonElement>("""
            () => Array.from(document.querySelectorAll('svg')).map(svg => ({
                ariaLabel: svg.getAttribute('aria-label') || '',
                ariaLabelledBy: svg.getAttribute('aria-labelledby') || '',
                role: svg.getAttribute('role') || '',
                hasTitle: !!svg.querySelector('title'),
                html: svg.outerHTML.substring(0, 200)
            }))
        """);

        var svgs = new List<SvgInfo>();
        foreach (var svg in svgsJson.EnumerateArray())
        {
            svgs.Add(new SvgInfo(
                svg.GetProperty("ariaLabel").GetString() ?? "",
                svg.GetProperty("ariaLabelledBy").GetString() ?? "",
                svg.GetProperty("role").GetString() ?? "",
                svg.GetProperty("hasTitle").GetBoolean(),
                svg.GetProperty("html").GetString() ?? ""));
        }

        return AnalyzeSvgImages(svgs);
    }

    internal static List<AccessibilityViolation> AnalyzeSvgImages(List<SvgInfo> svgs)
    {
        var violations = new List<AccessibilityViolation>();

        foreach (var svg in svgs)
        {
            var hasAccessibleName = !string.IsNullOrWhiteSpace(svg.AriaLabel)
                || !string.IsNullOrWhiteSpace(svg.AriaLabelledBy)
                || (svg.Role == "img" && svg.HasTitle);

            if (!hasAccessibleName)
            {
                violations.Add(new AccessibilityViolation
                {
                    RuleId = "svg-image-missing-alt",
                    Impact = "serious",
                    Description = "SVG image must have an accessible name via aria-label, aria-labelledby, or role=\"img\" with a <title> element (WCAG 1.1.1)",
                    HtmlElement = svg.Html
                });
            }
        }

        return violations;
    }

    private static async Task<List<AccessibilityViolation>> CheckEmptyLinksAsync(IPage page)
    {
        var linksJson = await page.EvaluateAsync<JsonElement>("""
            () => Array.from(document.querySelectorAll('a[href]')).map(a => ({
                text: (a.textContent || '').trim(),
                ariaLabel: a.getAttribute('aria-label') || '',
                ariaLabelledBy: a.getAttribute('aria-labelledby') || '',
                html: a.outerHTML.substring(0, 200)
            }))
        """);

        var links = new List<LinkInfo>();
        foreach (var link in linksJson.EnumerateArray())
        {
            links.Add(new LinkInfo(
                link.GetProperty("text").GetString() ?? "",
                link.GetProperty("ariaLabel").GetString() ?? "",
                link.GetProperty("ariaLabelledBy").GetString() ?? "",
                link.GetProperty("html").GetString() ?? ""));
        }

        return AnalyzeEmptyLinks(links);
    }

    internal static List<AccessibilityViolation> AnalyzeEmptyLinks(List<LinkInfo> links)
    {
        var violations = new List<AccessibilityViolation>();

        foreach (var link in links)
        {
            var hasAccessibleName = !string.IsNullOrWhiteSpace(link.Text)
                || !string.IsNullOrWhiteSpace(link.AriaLabel)
                || !string.IsNullOrWhiteSpace(link.AriaLabelledBy);

            if (!hasAccessibleName)
            {
                violations.Add(new AccessibilityViolation
                {
                    RuleId = "link-empty",
                    Impact = "serious",
                    Description = "Links must have discernible text so screen reader users know their purpose (WCAG 2.4.4)",
                    HtmlElement = link.Html
                });
            }
        }

        return violations;
    }

    private static async Task<AccessibilityViolation?> CheckMetaViewportAsync(IPage page)
    {
        var content = await page.EvaluateAsync<string>("""
            () => {
                const meta = document.querySelector('meta[name="viewport"]');
                return meta ? meta.getAttribute('content') || '' : '';
            }
        """);

        return AnalyzeMetaViewport(content);
    }

    internal static AccessibilityViolation? AnalyzeMetaViewport(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var disablesZoom = content.Contains("user-scalable=no")
            || content.Contains("user-scalable=0");

        if (disablesZoom)
        {
            return new AccessibilityViolation
            {
                RuleId = "meta-viewport-zoom-disabled",
                Impact = "critical",
                Description = "Viewport meta must not disable zoom — users with low vision need to scale content (WCAG 1.4.4)"
            };
        }

        return null;
    }

    private static async Task<List<AccessibilityViolation>> CheckEmptyButtonsAsync(IPage page)
    {
        var buttonsJson = await page.EvaluateAsync<JsonElement>("""
            () => Array.from(document.querySelectorAll('button')).map(btn => ({
                text: (btn.textContent || '').trim(),
                ariaLabel: btn.getAttribute('aria-label') || '',
                ariaLabelledBy: btn.getAttribute('aria-labelledby') || '',
                html: btn.outerHTML.substring(0, 200)
            }))
        """);

        var buttons = new List<ButtonInfo>();
        foreach (var btn in buttonsJson.EnumerateArray())
        {
            buttons.Add(new ButtonInfo(
                btn.GetProperty("text").GetString() ?? "",
                btn.GetProperty("ariaLabel").GetString() ?? "",
                btn.GetProperty("ariaLabelledBy").GetString() ?? "",
                btn.GetProperty("html").GetString() ?? ""));
        }

        return AnalyzeEmptyButtons(buttons);
    }

    internal static List<AccessibilityViolation> AnalyzeEmptyButtons(List<ButtonInfo> buttons)
    {
        var violations = new List<AccessibilityViolation>();

        foreach (var button in buttons)
        {
            var hasAccessibleName = !string.IsNullOrWhiteSpace(button.Text)
                || !string.IsNullOrWhiteSpace(button.AriaLabel)
                || !string.IsNullOrWhiteSpace(button.AriaLabelledBy);

            if (!hasAccessibleName)
            {
                violations.Add(new AccessibilityViolation
                {
                    RuleId = "button-empty",
                    Impact = "serious",
                    Description = "Buttons must have discernible text so screen reader users know their purpose (WCAG 4.1.2)",
                    HtmlElement = button.Html
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

internal record HeadingInfo(int Level, string OuterHtml);
internal record SkipLinkInfo(string Href, string Text);
internal record FormInputInfo(string Id, string Type, string AriaLabel, string AriaLabelledBy, bool HasLabel, string Html);
internal record DocumentTitleInfo(string Title);
internal record SvgInfo(string AriaLabel, string AriaLabelledBy, string Role, bool HasTitle, string Html);
internal record LinkInfo(string Text, string AriaLabel, string AriaLabelledBy, string Html);
internal record ButtonInfo(string Text, string AriaLabel, string AriaLabelledBy, string Html);
