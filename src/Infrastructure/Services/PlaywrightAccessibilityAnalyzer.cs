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

            var skipNavViolation = await CheckSkipNavigationAsync(page);
            if (skipNavViolation is not null)
                violations.Add(skipNavViolation);

            var formViolations = await CheckFormInputLabelsAsync(page);
            violations.AddRange(formViolations);

            var svgViolations = await CheckSvgImagesAsync(page);
            violations.AddRange(svgViolations);

            var tabindexViolations = await CheckTabindexAsync(page);
            violations.AddRange(tabindexViolations);

            var focusViolations = await CheckFocusVisibleAsync(page);
            violations.AddRange(focusViolations);

            var reflowViolation = await CheckReflowAsync(page);
            if (reflowViolation is not null)
                violations.Add(reflowViolation);

            var trapViolation = await CheckKeyboardTrapAsync(page);
            if (trapViolation is not null)
                violations.Add(trapViolation);

            var touchTargetViolations = await CheckTouchTargetSizeAsync(page);
            violations.AddRange(touchTargetViolations);

            var reducedMotionViolation = await CheckReducedMotionAsync(page);
            if (reducedMotionViolation is not null)
                violations.Add(reducedMotionViolation);

            var langViolation = await CheckLangAttributeValidAsync(page);
            if (langViolation is not null)
                violations.Add(langViolation);

            var autocompleteViolations = await CheckAutocompleteAsync(page);
            violations.AddRange(autocompleteViolations);

            var tableViolations = await CheckTableCaptionAsync(page);
            violations.AddRange(tableViolations);

            var pageTitleViolation = await CheckPageTitleDescriptiveAsync(page);
            if (pageTitleViolation is not null)
                violations.Add(pageTitleViolation);

            var selectTextareaViolations = await CheckSelectTextareaLabelsAsync(page);
            violations.AddRange(selectTextareaViolations);

            var fieldsetViolations = await CheckFieldsetLegendAsync(page);
            violations.AddRange(fieldsetViolations);

            var ariaLiveViolation = await CheckAriaLiveAsync(page);
            if (ariaLiveViolation is not null)
                violations.Add(ariaLiveViolation);

            var focusContextViolations = await CheckFocusContextChangeAsync(page);
            violations.AddRange(focusContextViolations);

            var errorIdentificationViolations = await CheckErrorIdentificationAsync(page);
            violations.AddRange(errorIdentificationViolations);

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

    private static async Task<List<AccessibilityViolation>> CheckTabindexAsync(IPage page)
    {
        var elementsJson = await page.EvaluateAsync<JsonElement>("""
            () => {
                const interactive = ['a', 'button', 'input', 'select', 'textarea'];
                return Array.from(document.querySelectorAll('[tabindex]')).map(el => ({
                    tag: el.tagName.toLowerCase(),
                    tabindex: parseInt(el.getAttribute('tabindex')),
                    isInteractive: interactive.includes(el.tagName.toLowerCase()),
                    html: el.outerHTML.substring(0, 200)
                }));
            }
        """);

        var elements = new List<TabindexInfo>();
        foreach (var el in elementsJson.EnumerateArray())
        {
            elements.Add(new TabindexInfo(
                el.GetProperty("tag").GetString() ?? "",
                el.GetProperty("tabindex").GetInt32(),
                el.GetProperty("isInteractive").GetBoolean(),
                el.GetProperty("html").GetString() ?? ""));
        }

        return AnalyzeTabindex(elements);
    }

    internal static List<AccessibilityViolation> AnalyzeTabindex(List<TabindexInfo> elements)
    {
        var violations = new List<AccessibilityViolation>();

        foreach (var el in elements)
        {
            if (el.Tabindex > 0)
            {
                violations.Add(new AccessibilityViolation
                {
                    RuleId = "tabindex-positive",
                    Impact = "moderate",
                    Description = $"Element has tabindex=\"{el.Tabindex}\" which disrupts the natural tab order for keyboard users (WCAG 2.4.3)",
                    HtmlElement = el.Html
                });
            }

            if (el.Tabindex == -1 && el.IsInteractive)
            {
                violations.Add(new AccessibilityViolation
                {
                    RuleId = "interactive-not-focusable",
                    Impact = "serious",
                    Description = $"Interactive <{el.Tag}> element has tabindex=\"-1\" making it unreachable by keyboard (WCAG 2.1.1)",
                    HtmlElement = el.Html
                });
            }
        }

        return violations;
    }

    private static async Task<List<AccessibilityViolation>> CheckFocusVisibleAsync(IPage page)
    {
        var elementsJson = await page.EvaluateAsync<JsonElement>("""
            () => {
                const sel = 'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex="0"]';
                const elements = Array.from(document.querySelectorAll(sel));
                return elements.slice(0, 20).map(el => {
                    el.focus();
                    const style = window.getComputedStyle(el);
                    return {
                        tag: el.tagName.toLowerCase(),
                        outlineWidth: style.outlineWidth,
                        outlineStyle: style.outlineStyle,
                        outlineColor: style.outlineColor,
                        html: el.outerHTML.substring(0, 200)
                    };
                });
            }
        """);

        var elements = new List<FocusInfo>();
        foreach (var el in elementsJson.EnumerateArray())
        {
            elements.Add(new FocusInfo(
                el.GetProperty("tag").GetString() ?? "",
                el.GetProperty("outlineWidth").GetString() ?? "",
                el.GetProperty("outlineStyle").GetString() ?? "",
                el.GetProperty("outlineColor").GetString() ?? "",
                el.GetProperty("html").GetString() ?? ""));
        }

        return AnalyzeFocusVisible(elements);
    }

    internal static List<AccessibilityViolation> AnalyzeFocusVisible(List<FocusInfo> elements)
    {
        var violations = new List<AccessibilityViolation>();

        foreach (var el in elements)
        {
            var noOutline = el.OutlineStyle == "none"
                || el.OutlineWidth == "0px"
                || el.OutlineStyle == "hidden";

            if (noOutline)
            {
                violations.Add(new AccessibilityViolation
                {
                    RuleId = "focus-visible-missing",
                    Impact = "serious",
                    Description = $"Element <{el.Tag}> has no visible focus indicator — keyboard users cannot see where focus is (WCAG 2.4.7)",
                    HtmlElement = el.Html
                });
            }
        }

        return violations;
    }

    private static async Task<AccessibilityViolation?> CheckReflowAsync(IPage page)
    {
        await page.SetViewportSizeAsync(320, 256);
        await page.WaitForLoadStateAsync(LoadState.Load);

        var scrollInfo = await page.EvaluateAsync<JsonElement>("""
            () => ({
                scrollWidth: document.documentElement.scrollWidth,
                clientWidth: document.documentElement.clientWidth
            })
        """);

        var scrollWidth = scrollInfo.GetProperty("scrollWidth").GetInt32();
        var clientWidth = scrollInfo.GetProperty("clientWidth").GetInt32();

        return AnalyzeReflow(scrollWidth, clientWidth);
    }

    internal static AccessibilityViolation? AnalyzeReflow(int scrollWidth, int clientWidth)
    {
        if (scrollWidth > clientWidth)
        {
            return new AccessibilityViolation
            {
                RuleId = "reflow-horizontal-scroll",
                Impact = "critical",
                Description = "Page requires horizontal scrolling at 320px width — users with 400% zoom cannot access all content (WCAG 1.4.10)"
            };
        }

        return null;
    }

    private static async Task<AccessibilityViolation?> CheckKeyboardTrapAsync(IPage page)
    {
        var maxTabs = 50;
        var focusedElements = new List<string>();

        await page.Keyboard.PressAsync("Tab");

        for (var i = 0; i < maxTabs; i++)
        {
            var focused = await page.EvaluateAsync<string>("""
                () => {
                    const el = document.activeElement;
                    if (!el || el === document.body) return '';
                    return el.tagName.toLowerCase() + (el.id ? '#' + el.id : '') + (el.className ? '.' + el.className.trim().replace(/\s+/g, '.') : '');
                }
            """);

            if (string.IsNullOrEmpty(focused))
                break;

            // If we've seen this element before and it's not the natural cycle end — trap detected
            var previousCount = focusedElements.Count(e => e == focused);
            if (previousCount >= 2)
                return AnalyzeKeyboardTrap(trapped: true, elementIdentifier: focused);

            focusedElements.Add(focused);
            await page.Keyboard.PressAsync("Tab");
        }

        return AnalyzeKeyboardTrap(trapped: false, elementIdentifier: "");
    }

    internal static AccessibilityViolation? AnalyzeKeyboardTrap(bool trapped, string elementIdentifier)
    {
        if (!trapped)
            return null;

        return new AccessibilityViolation
        {
            RuleId = "keyboard-trap",
            Impact = "critical",
            Description = $"Keyboard focus is trapped — users cannot navigate past element '{elementIdentifier}' using Tab (WCAG 2.1.2)"
        };
    }

    private static async Task<List<AccessibilityViolation>> CheckTouchTargetSizeAsync(IPage page)
    {
        var elementsJson = await page.EvaluateAsync<JsonElement>("""
            () => Array.from(document.querySelectorAll('a, button, input, select, textarea, [role="button"], [role="link"]'))
                .filter(el => {
                    const style = window.getComputedStyle(el);
                    return style.display !== 'none' && style.visibility !== 'hidden';
                })
                .map(el => {
                    const rect = el.getBoundingClientRect();
                    return {
                        tag: el.tagName.toLowerCase(),
                        width: rect.width,
                        height: rect.height,
                        html: el.outerHTML.substring(0, 200)
                    };
                })
        """);

        var elements = new List<TouchTargetInfo>();
        foreach (var el in elementsJson.EnumerateArray())
        {
            elements.Add(new TouchTargetInfo(
                el.GetProperty("tag").GetString() ?? "",
                el.GetProperty("width").GetDouble(),
                el.GetProperty("height").GetDouble(),
                el.GetProperty("html").GetString() ?? ""));
        }

        return AnalyzeTouchTargetSize(elements);
    }

    internal static List<AccessibilityViolation> AnalyzeTouchTargetSize(List<TouchTargetInfo> elements)
    {
        const int MinSize = 44;
        var violations = new List<AccessibilityViolation>();

        foreach (var el in elements)
        {
            if (el.Width < MinSize || el.Height < MinSize)
            {
                violations.Add(new AccessibilityViolation
                {
                    RuleId = "touch-target-too-small",
                    Impact = "moderate",
                    Description = $"Interactive <{el.Tag}> element is {el.Width:F0}×{el.Height:F0}px — touch target must be at least 44×44px for users with motor impairments (WCAG 2.5.5)",
                    HtmlElement = el.Html
                });
            }
        }

        return violations;
    }

    private static async Task<AccessibilityViolation?> CheckReducedMotionAsync(IPage page)
    {
        var hasAnimations = await page.EvaluateAsync<bool>("""
            () => {
                const sheets = Array.from(document.styleSheets);
                const hasAnimation = sheets.some(sheet => {
                    try {
                        return Array.from(sheet.cssRules).some(rule =>
                            rule.cssText && (
                                rule.cssText.includes('animation') ||
                                rule.cssText.includes('transition')
                            )
                        );
                    } catch { return false; }
                });

                const hasReducedMotion = sheets.some(sheet => {
                    try {
                        return Array.from(sheet.cssRules).some(rule =>
                            rule.conditionText &&
                            rule.conditionText.includes('prefers-reduced-motion')
                        );
                    } catch { return false; }
                });

                return hasAnimation && !hasReducedMotion;
            }
        """);

        return AnalyzeReducedMotion(hasAnimations);
    }

    internal static AccessibilityViolation? AnalyzeReducedMotion(bool hasAnimationsWithoutReducedMotion)
    {
        if (!hasAnimationsWithoutReducedMotion)
            return null;

        return new AccessibilityViolation
        {
            RuleId = "animation-reduced-motion-missing",
            Impact = "moderate",
            Description = "Page uses animations or transitions but does not respect prefers-reduced-motion — users with vestibular disorders may be harmed (WCAG 2.3.3)"
        };
    }

    private static async Task<AccessibilityViolation?> CheckLangAttributeValidAsync(IPage page)
    {
        var lang = await page.EvaluateAsync<string>("() => document.documentElement.getAttribute('lang') || ''");
        return AnalyzeLangAttributeValid(lang);
    }

    internal static AccessibilityViolation? AnalyzeLangAttributeValid(string lang)
    {
        if (string.IsNullOrWhiteSpace(lang))
            return null; // missing lang is handled by axe-core html-has-lang rule

        // BCP 47: primary language subtag is 2-3 lowercase letters, optionally followed by subtags
        // Valid examples: "en", "pl", "en-US", "zh-Hans", "sr-Latn-RS"
        // Invalid examples: "english", "Polish", "123", "e"
        var primaryTag = lang.Split('-')[0];
        var isValid = primaryTag.Length >= 2
            && primaryTag.Length <= 3
            && primaryTag.All(char.IsLetter);

        if (!isValid)
        {
            return new AccessibilityViolation
            {
                RuleId = "lang-attribute-invalid",
                Impact = "serious",
                Description = $"The lang attribute value \"{lang}\" is not a valid BCP 47 language tag — screen readers may use the wrong language for text-to-speech (WCAG 3.1.1)"
            };
        }

        return null;
    }

    private static async Task<List<AccessibilityViolation>> CheckAutocompleteAsync(IPage page)
    {
        var inputsJson = await page.EvaluateAsync<JsonElement>("""
            () => {
                const personalTypes = ['name', 'email', 'tel', 'address', 'city', 'country', 'zip', 'postal'];
                const personalNames = ['name', 'email', 'phone', 'tel', 'address', 'city', 'country', 'zip', 'postal', 'firstname', 'lastname', 'surname'];
                return Array.from(document.querySelectorAll('input[type="text"], input[type="email"], input[type="tel"], input:not([type])'))
                    .filter(i => {
                        const name = (i.name || i.id || i.placeholder || '').toLowerCase();
                        return personalNames.some(p => name.includes(p));
                    })
                    .map(i => ({
                        autocomplete: i.getAttribute('autocomplete') || '',
                        name: i.name || i.id || '',
                        html: i.outerHTML.substring(0, 200)
                    }));
            }
        """);

        var inputs = new List<AutocompleteInfo>();
        foreach (var input in inputsJson.EnumerateArray())
        {
            inputs.Add(new AutocompleteInfo(
                input.GetProperty("autocomplete").GetString() ?? "",
                input.GetProperty("name").GetString() ?? "",
                input.GetProperty("html").GetString() ?? ""));
        }

        return AnalyzeAutocomplete(inputs);
    }

    internal static List<AccessibilityViolation> AnalyzeAutocomplete(List<AutocompleteInfo> inputs)
    {
        var violations = new List<AccessibilityViolation>();

        foreach (var input in inputs)
        {
            if (string.IsNullOrWhiteSpace(input.Autocomplete) || input.Autocomplete == "off")
            {
                violations.Add(new AccessibilityViolation
                {
                    RuleId = "autocomplete-missing",
                    Impact = "serious",
                    Description = $"Input field '{input.Name}' collects personal data but is missing the autocomplete attribute — users with cognitive or motor impairments rely on browser autofill (WCAG 1.3.5)",
                    HtmlElement = input.Html
                });
            }
        }

        return violations;
    }

    private static async Task<List<AccessibilityViolation>> CheckTableCaptionAsync(IPage page)
    {
        var tablesJson = await page.EvaluateAsync<JsonElement>("""
            () => Array.from(document.querySelectorAll('table')).map(table => ({
                hasCaption: !!table.querySelector('caption'),
                hasSummary: !!table.getAttribute('summary'),
                hasAriaLabel: !!table.getAttribute('aria-label'),
                hasAriaLabelledBy: !!table.getAttribute('aria-labelledby'),
                html: table.outerHTML.substring(0, 200)
            }))
        """);

        var tables = new List<TableInfo>();
        foreach (var table in tablesJson.EnumerateArray())
        {
            tables.Add(new TableInfo(
                table.GetProperty("hasCaption").GetBoolean(),
                table.GetProperty("hasSummary").GetBoolean(),
                table.GetProperty("hasAriaLabel").GetBoolean(),
                table.GetProperty("hasAriaLabelledBy").GetBoolean(),
                table.GetProperty("html").GetString() ?? ""));
        }

        return AnalyzeTableCaption(tables);
    }

    internal static List<AccessibilityViolation> AnalyzeTableCaption(List<TableInfo> tables)
    {
        var violations = new List<AccessibilityViolation>();

        foreach (var table in tables)
        {
            var hasAccessibleName = table.HasCaption
                || table.HasSummary
                || table.HasAriaLabel
                || table.HasAriaLabelledBy;

            if (!hasAccessibleName)
            {
                violations.Add(new AccessibilityViolation
                {
                    RuleId = "table-missing-caption",
                    Impact = "moderate",
                    Description = "Data table has no caption or accessible name — screen reader users cannot understand the table's purpose without reading all its content (WCAG 1.3.1)",
                    HtmlElement = table.Html
                });
            }
        }

        return violations;
    }

    private static async Task<AccessibilityViolation?> CheckPageTitleDescriptiveAsync(IPage page)
    {
        var title = await page.EvaluateAsync<string>("() => document.title || ''");
        return AnalyzePageTitleDescriptive(title);
    }

    internal static AccessibilityViolation? AnalyzePageTitleDescriptive(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return null; // missing title handled by axe-core document-title rule

        var genericTitles = new[] { "home", "index", "untitled", "page", "new page", "welcome", "default" };
        var normalized = title.Trim().ToLowerInvariant();

        if (normalized.Length < 4 || genericTitles.Any(g => normalized == g))
        {
            return new AccessibilityViolation
            {
                RuleId = "page-title-not-descriptive",
                Impact = "moderate",
                Description = $"Page title \"{title}\" is too generic — titles should describe the page content so users can distinguish tabs and navigate history (WCAG 2.4.2)"
            };
        }

        return null;
    }

    private static async Task<List<AccessibilityViolation>> CheckSelectTextareaLabelsAsync(IPage page)
    {
        var elementsJson = await page.EvaluateAsync<JsonElement>("""
            () => Array.from(document.querySelectorAll('select, textarea')).map(el => ({
                tag: el.tagName.toLowerCase(),
                id: el.id || '',
                ariaLabel: el.getAttribute('aria-label') || '',
                ariaLabelledBy: el.getAttribute('aria-labelledby') || '',
                hasLabel: el.id ? !!document.querySelector(`label[for="${el.id}"]`) : false,
                html: el.outerHTML.substring(0, 200)
            }))
        """);

        var elements = new List<SelectTextareaInfo>();
        foreach (var el in elementsJson.EnumerateArray())
        {
            elements.Add(new SelectTextareaInfo(
                el.GetProperty("tag").GetString() ?? "",
                el.GetProperty("ariaLabel").GetString() ?? "",
                el.GetProperty("ariaLabelledBy").GetString() ?? "",
                el.GetProperty("hasLabel").GetBoolean(),
                el.GetProperty("html").GetString() ?? ""));
        }

        return AnalyzeSelectTextareaLabels(elements);
    }

    internal static List<AccessibilityViolation> AnalyzeSelectTextareaLabels(List<SelectTextareaInfo> elements)
    {
        var violations = new List<AccessibilityViolation>();

        foreach (var el in elements)
        {
            var hasAccessibleName = el.HasLabel
                || !string.IsNullOrWhiteSpace(el.AriaLabel)
                || !string.IsNullOrWhiteSpace(el.AriaLabelledBy);

            if (!hasAccessibleName)
            {
                violations.Add(new AccessibilityViolation
                {
                    RuleId = "select-textarea-missing-label",
                    Impact = "critical",
                    Description = $"<{el.Tag}> element has no associated label, aria-label, or aria-labelledby — screen reader users cannot identify its purpose (WCAG 1.3.1)",
                    HtmlElement = el.Html
                });
            }
        }

        return violations;
    }

    private static async Task<List<AccessibilityViolation>> CheckFieldsetLegendAsync(IPage page)
    {
        var fieldsetsJson = await page.EvaluateAsync<JsonElement>("""
            () => Array.from(document.querySelectorAll('fieldset')).map(fs => ({
                hasLegend: !!fs.querySelector('legend'),
                legendText: (fs.querySelector('legend')?.textContent || '').trim(),
                hasAriaLabel: !!fs.getAttribute('aria-label'),
                hasAriaLabelledBy: !!fs.getAttribute('aria-labelledby'),
                html: fs.outerHTML.substring(0, 200)
            }))
        """);

        var fieldsets = new List<FieldsetInfo>();
        foreach (var fs in fieldsetsJson.EnumerateArray())
        {
            fieldsets.Add(new FieldsetInfo(
                fs.GetProperty("hasLegend").GetBoolean(),
                fs.GetProperty("legendText").GetString() ?? "",
                fs.GetProperty("hasAriaLabel").GetBoolean(),
                fs.GetProperty("hasAriaLabelledBy").GetBoolean(),
                fs.GetProperty("html").GetString() ?? ""));
        }

        return AnalyzeFieldsetLegend(fieldsets);
    }

    internal static List<AccessibilityViolation> AnalyzeFieldsetLegend(List<FieldsetInfo> fieldsets)
    {
        var violations = new List<AccessibilityViolation>();

        foreach (var fs in fieldsets)
        {
            var hasAccessibleName = (fs.HasLegend && !string.IsNullOrWhiteSpace(fs.LegendText))
                || fs.HasAriaLabel
                || fs.HasAriaLabelledBy;

            if (!hasAccessibleName)
            {
                violations.Add(new AccessibilityViolation
                {
                    RuleId = "fieldset-missing-legend",
                    Impact = "serious",
                    Description = "A <fieldset> grouping related form controls has no <legend> or accessible name — screen reader users cannot understand what the group represents (WCAG 1.3.1)",
                    HtmlElement = fs.Html
                });
            }
        }

        return violations;
    }

    private static async Task<AccessibilityViolation?> CheckAriaLiveAsync(IPage page)
    {
        var result = await page.EvaluateAsync<JsonElement>("""
            () => {
                const alertRoles = ['alert', 'status', 'log', 'marquee', 'timer'];
                const hasAlertElements = document.querySelectorAll(
                    '[role="alert"], [role="status"], [role="log"], .alert, .error, .notification, .toast, .snackbar'
                ).length > 0;

                const hasAriaLive = document.querySelectorAll('[aria-live]').length > 0;
                const hasRoleWithImplicitLive = document.querySelectorAll(
                    '[role="alert"], [role="status"], [role="log"]'
                ).length > 0;

                return {
                    hasAlertElements,
                    hasAriaLive,
                    hasRoleWithImplicitLive
                };
            }
        """);

        var hasAlertElements = result.GetProperty("hasAlertElements").GetBoolean();
        var hasAriaLive = result.GetProperty("hasAriaLive").GetBoolean();
        var hasRoleWithImplicitLive = result.GetProperty("hasRoleWithImplicitLive").GetBoolean();

        return AnalyzeAriaLive(hasAlertElements, hasAriaLive || hasRoleWithImplicitLive);
    }

    internal static AccessibilityViolation? AnalyzeAriaLive(bool hasAlertElements, bool hasLiveRegion)
    {
        if (!hasAlertElements || hasLiveRegion)
            return null;

        return new AccessibilityViolation
        {
            RuleId = "aria-live-missing",
            Impact = "serious",
            Description = "Page contains alert or notification elements but no aria-live regions — dynamic status messages are invisible to screen reader users (WCAG 4.1.3)"
        };
    }

    private static async Task<List<AccessibilityViolation>> CheckFocusContextChangeAsync(IPage page)
    {
        var initialUrl = page.Url;
        var focusableSelectors = "a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex=\"0\"]";

        var elementsJson = await page.EvaluateAsync<JsonElement>("""
            () => {
                const sel = 'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex="0"]';
                return Array.from(document.querySelectorAll(sel)).slice(0, 15).map(el => ({
                    html: el.outerHTML.substring(0, 200)
                }));
            }
        """);

        var violations = new List<FocusContextInfo>();

        var index = 0;
        foreach (var el in elementsJson.EnumerateArray())
        {
            var html = el.GetProperty("html").GetString() ?? "";

            var urlBefore = page.Url;
            await page.EvaluateAsync($"() => document.querySelectorAll('a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex=\"0\"]')[{index}]?.focus()");
            await page.WaitForTimeoutAsync(200);
            var urlAfter = page.Url;

            if (urlBefore != urlAfter)
            {
                violations.Add(new FocusContextInfo(html, urlBefore, urlAfter));
                await page.GotoAsync(urlBefore);
            }

            index++;
        }

        return AnalyzeFocusContextChange(violations);
    }

    internal static List<AccessibilityViolation> AnalyzeFocusContextChange(List<FocusContextInfo> items)
    {
        return items.Select(item => new AccessibilityViolation
        {
            RuleId = "focus-causes-context-change",
            Impact = "serious",
            Description = $"Focusing an element triggered a page navigation from \"{item.UrlBefore}\" to \"{item.UrlAfter}\" — context changes on focus disorient keyboard users (WCAG 3.2.1)",
            HtmlElement = item.Html
        }).ToList();
    }

    private static async Task<List<AccessibilityViolation>> CheckErrorIdentificationAsync(IPage page)
    {
        var inputsJson = await page.EvaluateAsync<JsonElement>("""
            () => Array.from(document.querySelectorAll('input[required], select[required], textarea[required], [aria-required="true"]'))
                .map(el => ({
                    tag: el.tagName.toLowerCase(),
                    ariaInvalid: el.getAttribute('aria-invalid') || '',
                    ariaDescribedBy: el.getAttribute('aria-describedby') || '',
                    id: el.id || '',
                    hasErrorMessage: !!el.getAttribute('aria-describedby') &&
                        !!document.getElementById(el.getAttribute('aria-describedby') || ''),
                    html: el.outerHTML.substring(0, 200)
                }))
        """);

        var inputs = new List<RequiredInputInfo>();
        foreach (var input in inputsJson.EnumerateArray())
        {
            inputs.Add(new RequiredInputInfo(
                input.GetProperty("tag").GetString() ?? "",
                input.GetProperty("ariaInvalid").GetString() ?? "",
                input.GetProperty("ariaDescribedBy").GetString() ?? "",
                input.GetProperty("hasErrorMessage").GetBoolean(),
                input.GetProperty("html").GetString() ?? ""));
        }

        return AnalyzeErrorIdentification(inputs);
    }

    internal static List<AccessibilityViolation> AnalyzeErrorIdentification(List<RequiredInputInfo> inputs)
    {
        var violations = new List<AccessibilityViolation>();

        foreach (var input in inputs)
        {
            var isMarkedInvalid = input.AriaInvalid == "true";
            var hasLinkedErrorMessage = input.HasErrorMessage;

            if (isMarkedInvalid && !hasLinkedErrorMessage)
            {
                violations.Add(new AccessibilityViolation
                {
                    RuleId = "error-missing-description",
                    Impact = "serious",
                    Description = $"<{input.Tag}> is marked aria-invalid=\"true\" but has no aria-describedby pointing to an error message — screen reader users know the field is wrong but not why (WCAG 3.3.1)",
                    HtmlElement = input.Html
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
internal record SvgInfo(string AriaLabel, string AriaLabelledBy, string Role, bool HasTitle, string Html);
internal record TabindexInfo(string Tag, int Tabindex, bool IsInteractive, string Html);
internal record FocusInfo(string Tag, string OutlineWidth, string OutlineStyle, string OutlineColor, string Html);
internal record TouchTargetInfo(string Tag, double Width, double Height, string Html);
internal record AutocompleteInfo(string Autocomplete, string Name, string Html);
internal record TableInfo(bool HasCaption, bool HasSummary, bool HasAriaLabel, bool HasAriaLabelledBy, string Html);
internal record SelectTextareaInfo(string Tag, string AriaLabel, string AriaLabelledBy, bool HasLabel, string Html);
internal record FieldsetInfo(bool HasLegend, string LegendText, bool HasAriaLabel, bool HasAriaLabelledBy, string Html);
internal record FocusContextInfo(string Html, string UrlBefore, string UrlAfter);
internal record RequiredInputInfo(string Tag, string AriaInvalid, string AriaDescribedBy, bool HasErrorMessage, string Html);
