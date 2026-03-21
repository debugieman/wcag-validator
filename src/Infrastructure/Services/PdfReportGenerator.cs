using System.Reflection;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WcagAnalyzer.Application.Features.Analysis.Queries;
using WcagAnalyzer.Application.Services;

namespace WcagAnalyzer.Infrastructure.Services;

public class PdfReportGenerator : IPdfReportGenerator
{
    private static readonly Dictionary<string, string> ImpactOrder = new()
    {
        ["critical"] = "Critical",
        ["serious"]  = "Serious",
        ["moderate"] = "Moderate",
        ["minor"]    = "Minor"
    };

    private static readonly Dictionary<string, string> ImpactColors = new()
    {
        ["critical"] = "#D32F2F",
        ["serious"]  = "#F57C00",
        ["moderate"] = "#F9A825",
        ["minor"]    = "#388E3C"
    };

    private static readonly Dictionary<string, string> RuleDescriptions = new()
    {
        ["color-contrast"]                  = "Text is too low-contrast against its background. Affects ~8% of users with visual impairments and anyone reading in bright sunlight.",
        ["image-alt"]                       = "Images have no alternative text. Screen readers cannot describe them to blind or visually impaired users.",
        ["svg-image-missing-alt"]           = "SVG graphics have no accessible label. Assistive technologies will skip or misread these images.",
        ["button-name"]                     = "Buttons have no descriptive label. Users relying on screen readers cannot tell what a button does.",
        ["link-name"]                       = "Links have no descriptive text. 'Click here' or icon-only links are meaningless to screen reader users.",
        ["input-missing-label"]             = "Form fields have no labels. Users with screen readers cannot tell what information is expected.",
        ["select-textarea-missing-label"]   = "Dropdown menus or text areas have no labels, making forms inaccessible to assistive technology users.",
        ["label"]                           = "Form inputs are missing programmatic labels required by screen readers.",
        ["html-has-lang"]                   = "The page has no language declaration. Screen readers cannot select the correct voice or pronunciation.",
        ["heading-level-skipped"]           = "Heading levels jump (e.g. H1 → H3). This breaks the logical structure of the page for screen reader users.",
        ["heading-first-not-h1"]            = "The first heading on the page is not H1. This confuses screen readers and search engines.",
        ["skip-navigation-missing"]         = "There is no 'Skip to content' link. Keyboard-only users must tab through every menu item on every page.",
        ["landmark-one-main"]               = "The page has no main landmark region. Screen reader users cannot jump directly to the main content.",
        ["landmark-unique"]                 = "Multiple identical landmark regions exist without labels, making navigation confusing.",
        ["region"]                          = "Page content is not organised into landmark regions (header, main, footer). Structural navigation is impossible.",
        ["list"]                            = "List elements are used incorrectly. This disrupts how screen readers announce list content.",
        ["table-missing-caption"]           = "Data tables have no caption or summary. Screen reader users cannot understand the table's purpose without reading all content first.",
        ["aria-allowed-role"]               = "ARIA roles are applied to elements where they are not permitted, causing unpredictable behaviour for assistive technologies.",
        ["focus-visible-missing"]           = "Keyboard focus is not visually visible. Keyboard-only users cannot see which element they are interacting with.",
        ["interactive-not-focusable"]       = "Interactive elements cannot be reached by keyboard. Users who cannot use a mouse are completely locked out.",
        ["keyboard-trap"]                   = "Keyboard focus gets trapped inside a component. Users who rely on the keyboard cannot escape or navigate away.",
        ["reflow-horizontal-scroll"]        = "The page requires horizontal scrolling at 320px width. Users with low vision who zoom in will struggle to read the content.",
        ["touch-target-too-small"]          = "Tap targets (buttons, links) are too small. Users with motor impairments or large fingers will frequently mis-tap.",
        ["animation-reduced-motion-missing"]= "Animations do not respect the user's 'reduce motion' system preference. This can trigger symptoms in users with vestibular disorders.",
        ["table-missing-caption"]           = "Data tables have no caption. Users with screen readers cannot identify the table's purpose.",
    };

    public byte[] Generate(GetAnalysisByIdResult analysis)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var logoBytes = TryLoadLogo();
        var grouped = GroupByImpact(analysis);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c, logoBytes, analysis));
                page.Content().Element(c => ComposeContent(c, analysis, grouped));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, byte[]? logoBytes, GetAnalysisByIdResult analysis)
    {
        var score = CalculateScore(analysis.Results.ToList());
        var scoreColor = score >= 80 ? "#388E3C" : score >= 50 ? "#F57C00" : "#D32F2F";

        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                if (logoBytes is not null)
                    row.ConstantItem(80).Image(logoBytes);

                row.RelativeItem().PaddingLeft(logoBytes is not null ? 16 : 0).Column(inner =>
                {
                    inner.Item().Text("WCAG Accessibility Report")
                        .FontSize(20).Bold().FontColor("#1A237E");
                    inner.Item().Text(analysis.Url)
                        .FontSize(10).FontColor("#546E7A");
                    inner.Item().Text($"Generated: {analysis.CompletedAt?.ToString("dd MMM yyyy HH:mm") ?? DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")} UTC")
                        .FontSize(9).FontColor("#90A4AE");
                });

                row.ConstantItem(80).AlignRight().AlignMiddle().Column(scoreCol =>
                {
                    scoreCol.Item().AlignCenter().Text($"{score}/100")
                        .FontSize(22).Bold().FontColor(scoreColor);
                    scoreCol.Item().AlignCenter().Text("Accessibility Score")
                        .FontSize(7).FontColor("#90A4AE");
                });
            });

            col.Item().PaddingTop(4).Column(bar =>
            {
                var filled = score;
                bar.Item().Row(row =>
                {
                    if (filled > 0)
                        row.RelativeItem(filled).Height(6).Background(scoreColor);
                    if (filled < 100)
                        row.RelativeItem(100 - filled).Height(6).Background("#ECEFF1");
                });
            });

            col.Item().PaddingTop(4).LineHorizontal(1).LineColor("#1A237E");
        });
    }

    private static int CalculateScore(List<AnalysisResultDto> results)
    {
        var deductions = results
            .GroupBy(r => r.RuleId)
            .Sum(g => g.First().Impact switch
            {
                "critical" => 10,
                "serious"  => 7,
                "moderate" => 4,
                "minor"    => 1,
                _          => 0
            });

        return Math.Max(0, 100 - deductions);
    }

    private static void ComposeContent(IContainer container, GetAnalysisByIdResult analysis, Dictionary<string, List<AnalysisResultDto>> grouped)
    {
        container.PaddingTop(16).Column(col =>
        {
            col.Item().Element(ComposeIntro);

            col.Item().PaddingTop(16);

            col.Item().Element(c => ComposeSummary(c, analysis.Results.ToList()));

            col.Item().PaddingTop(20);

            if (!analysis.Results.Any())
            {
                col.Item().Padding(20).AlignCenter()
                    .Text("No accessibility violations found.")
                    .FontSize(13).Bold().FontColor("#388E3C");
                return;
            }

            col.Item().Element(c => ComposeTopPriorities(c, analysis.Results.ToList()));

            col.Item().PaddingTop(20);

            foreach (var (impact, label) in ImpactOrder)
            {
                if (!grouped.TryGetValue(impact, out var items) || items.Count == 0)
                    continue;

                col.Item().Element(c => ComposeSection(c, label, impact, items));
                col.Item().PaddingTop(16);
            }

            col.Item().Element(c => ComposeRecommendations(c, analysis.Results.ToList()));
        });
    }

    private static void ComposeIntro(IContainer container)
    {
        container.Border(1).BorderColor("#CFD8DC").Padding(14).Column(col =>
        {
            // Block 1: What is WCAG
            col.Item().Text("About this report").FontSize(12).Bold().FontColor("#1A237E");

            col.Item().PaddingTop(6).Text(text =>
            {
                text.Span("This report was generated by ")
                    .FontSize(9).FontColor("#37474F");
                text.Span("WCAG Analyzer")
                    .FontSize(9).Bold().FontColor("#1A237E");
                text.Span(" — an automated accessibility testing tool powered by axe-core and Playwright. It scans web pages against the ")
                    .FontSize(9).FontColor("#37474F");
                text.Span("Web Content Accessibility Guidelines (WCAG) 2.2")
                    .FontSize(9).Bold().FontColor("#37474F");
                text.Span(", the international standard for web accessibility published by the W3C.")
                    .FontSize(9).FontColor("#37474F");
            });

            col.Item().PaddingTop(6).Text(text =>
            {
                text.Span("WCAG defines three conformance levels: ")
                    .FontSize(9).FontColor("#37474F");
                text.Span("Level A ").FontSize(9).Bold().FontColor("#D32F2F");
                text.Span("(minimum), ").FontSize(9).FontColor("#37474F");
                text.Span("Level AA ").FontSize(9).Bold().FontColor("#F57C00");
                text.Span("(standard — required by most regulations), and ").FontSize(9).FontColor("#37474F");
                text.Span("Level AAA ").FontSize(9).Bold().FontColor("#388E3C");
                text.Span("(enhanced). Most organisations target Level AA compliance.")
                    .FontSize(9).FontColor("#37474F");
            });

            // Divider
            col.Item().PaddingTop(10).PaddingBottom(10).LineHorizontal(1).LineColor("#ECEFF1");

            // Block 2: Legal obligations
            col.Item().Text("Legal obligations").FontSize(11).Bold().FontColor("#1A237E");

            col.Item().PaddingTop(6).Text(text =>
            {
                text.Span("From June 2025, the ")
                    .FontSize(9).FontColor("#37474F");
                text.Span("European Accessibility Act (EAA)")
                    .FontSize(9).Bold().FontColor("#37474F");
                text.Span(" requires private sector organisations operating in the EU to make their digital products and services accessible. Non-compliance can result in ")
                    .FontSize(9).FontColor("#37474F");
                text.Span("fines, legal proceedings, and reputational damage.")
                    .FontSize(9).Bold().FontColor("#D32F2F");
                text.Span(" Public sector websites have been subject to similar obligations since 2018 under the EU Web Accessibility Directive.")
                    .FontSize(9).FontColor("#37474F");
            });

            // Divider
            col.Item().PaddingTop(10).PaddingBottom(10).LineHorizontal(1).LineColor("#ECEFF1");

            // Block 3: How to prioritize
            col.Item().Text("How to prioritize fixes").FontSize(11).Bold().FontColor("#1A237E");

            col.Item().PaddingTop(6).Row(row =>
            {
                PriorityBadge(row.RelativeItem(), "Critical", "#D32F2F", "#FFEBEE",
                    "Blocks access entirely. Fix immediately.");
                PriorityBadge(row.RelativeItem(), "Serious", "#F57C00", "#FFF3E0",
                    "Significant barrier. Fix before release.");
                PriorityBadge(row.RelativeItem(), "Moderate", "#F9A825", "#FFFDE7",
                    "Causes difficulty. Schedule for next sprint.");
                PriorityBadge(row.RelativeItem(), "Minor", "#388E3C", "#E8F5E9",
                    "Low impact. Fix when possible.");
            });
        });
    }

    private static void PriorityBadge(IContainer container, string label, string color, string bg, string description)
    {
        container.Padding(4).Column(col =>
        {
            col.Item().Background(bg).Padding(6).Column(inner =>
            {
                inner.Item().Text(label).FontSize(9).Bold().FontColor(color);
                inner.Item().PaddingTop(3).Text(description).FontSize(8).FontColor("#546E7A");
            });
        });
    }

    private static void ComposeSummary(IContainer container, List<AnalysisResultDto> results)
    {
        var total    = results.Count;
        var critical = results.Count(r => r.Impact == "critical");
        var serious  = results.Count(r => r.Impact == "serious");
        var moderate = results.Count(r => r.Impact == "moderate");
        var minor    = results.Count(r => r.Impact == "minor");

        container.Border(1).BorderColor("#CFD8DC").Padding(14).Column(col =>
        {
            col.Item().Text("Summary").FontSize(13).Bold().FontColor("#1A237E");
            col.Item().PaddingTop(8).Row(row =>
            {
                SummaryBadge(row.RelativeItem(), total.ToString(),    "Total",    "#1A237E", "#E8EAF6");
                SummaryBadge(row.RelativeItem(), critical.ToString(), "Critical", "#D32F2F", "#FFEBEE");
                SummaryBadge(row.RelativeItem(), serious.ToString(),  "Serious",  "#F57C00", "#FFF3E0");
                SummaryBadge(row.RelativeItem(), moderate.ToString(), "Moderate", "#F9A825", "#FFFDE7");
                SummaryBadge(row.RelativeItem(), minor.ToString(),    "Minor",    "#388E3C", "#E8F5E9");
            });
        });
    }

    private static void SummaryBadge(IContainer container, string count, string label, string textColor, string bgColor)
    {
        container.Padding(4).Background(bgColor).Padding(8).Column(col =>
        {
            col.Item().AlignCenter().Text(count).FontSize(20).Bold().FontColor(textColor);
            col.Item().AlignCenter().Text(label).FontSize(9).FontColor(textColor);
        });
    }

    private static void ComposeSection(IContainer container, string label, string impact, List<AnalysisResultDto> items)
    {
        var color = ImpactColors.GetValueOrDefault(impact, "#546E7A");
        var grouped = items
            .GroupBy(r => r.RuleId)
            .Select(g => new
            {
                RuleId      = g.Key,
                Count       = g.Count(),
                Description = g.First().Description,
                HelpUrl     = g.First().HelpUrl,
                Examples    = g.Where(r => !string.IsNullOrWhiteSpace(r.HtmlElement))
                               .Take(3)
                               .Select(r => r.HtmlElement!)
                               .ToList()
            })
            .ToList();

        container.Column(col =>
        {
            col.Item().Background(color).Padding(8).Row(row =>
            {
                row.RelativeItem().Text($"{label} Issues ({grouped.Count} unique rules, {items.Count} occurrences)")
                    .FontSize(11).Bold().FontColor(Colors.White);
            });

            foreach (var (group, index) in grouped.Select((g, i) => (g, i)))
            {
                var bg = index % 2 == 0 ? "#FAFAFA" : "#FFFFFF";

                col.Item().Background(bg).BorderBottom(1).BorderColor("#ECEFF1").Padding(10).Column(inner =>
                {
                    inner.Item().Row(row =>
                    {
                        row.RelativeItem().Text(group.RuleId).FontSize(10).Bold().FontColor("#263238");
                        row.ConstantItem(80).AlignRight()
                            .Text($"×{group.Count} occurrences")
                            .FontSize(8).FontColor(color);
                    });

                    inner.Item().PaddingTop(3).Text(group.Description)
                        .FontSize(9).FontColor("#546E7A");

                    if (group.Examples.Count > 0)
                    {
                        inner.Item().PaddingTop(5).Text($"Examples ({group.Examples.Count} of {group.Count}):")
                            .FontSize(8).FontColor("#90A4AE");

                        foreach (var example in group.Examples)
                        {
                            inner.Item().PaddingTop(3)
                                .Background("#F5F5F5").Padding(6)
                                .Text(TruncateHtml(example))
                                .FontSize(8).FontColor("#37474F");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(group.HelpUrl))
                    {
                        inner.Item().PaddingTop(4)
                            .Text($"More info: {group.HelpUrl}")
                            .FontSize(8).FontColor("#1565C0").Underline();
                    }
                });
            }
        });
    }

    private static void ComposeTopPriorities(IContainer container, List<AnalysisResultDto> results)
    {
        var top3 = results
            .GroupBy(r => r.RuleId)
            .Select(g => new { RuleId = g.Key, Impact = g.First().Impact, Count = g.Count() })
            .OrderBy(g => ImpactOrder.Keys.ToList().IndexOf(g.Impact))
            .ThenByDescending(g => g.Count)
            .Take(3)
            .ToList();

        if (top3.Count == 0) return;

        container.Border(1).BorderColor("#CFD8DC").Padding(14).Column(col =>
        {
            col.Item().Text("Top Priorities").FontSize(13).Bold().FontColor("#1A237E");
            col.Item().PaddingTop(4).Text("Fix these first for the biggest accessibility improvement.")
                .FontSize(9).FontColor("#546E7A");

            col.Item().PaddingTop(10);

            foreach (var (item, index) in top3.Select((x, i) => (x, i)))
            {
                var impactColor = ImpactColors.GetValueOrDefault(item.Impact, "#546E7A");
                var friendlyDesc = RuleDescriptions.GetValueOrDefault(item.RuleId, item.RuleId);

                col.Item().PaddingBottom(8).Row(row =>
                {
                    row.ConstantItem(24).AlignMiddle().AlignCenter()
                        .Background(impactColor).Padding(4)
                        .Text($"{index + 1}").FontSize(10).Bold().FontColor(Colors.White);

                    row.ConstantItem(8);

                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text(text =>
                        {
                            text.Span(item.RuleId).FontSize(10).Bold().FontColor("#263238");
                            text.Span($"  ×{item.Count}").FontSize(9).FontColor(impactColor);
                        });
                        inner.Item().PaddingTop(2).Text(friendlyDesc)
                            .FontSize(9).FontColor("#546E7A");
                    });
                });
            }
        });
    }

    private static void ComposeRecommendations(IContainer container, List<AnalysisResultDto> results)
    {
        var uniqueRules = results
            .GroupBy(r => r.RuleId)
            .Where(g => RuleDescriptions.ContainsKey(g.Key))
            .Select(g => new { RuleId = g.Key, Impact = g.First().Impact, Count = g.Count() })
            .OrderBy(g => ImpactOrder.Keys.ToList().IndexOf(g.Impact))
            .ToList();

        if (uniqueRules.Count == 0) return;

        container.Border(1).BorderColor("#CFD8DC").Padding(14).Column(col =>
        {
            col.Item().Text("What These Issues Mean for Your Business")
                .FontSize(13).Bold().FontColor("#1A237E");
            col.Item().PaddingTop(4).Text(
                "Below is a plain-language explanation of each issue found on your site and why it matters to your visitors.")
                .FontSize(9).FontColor("#546E7A");

            col.Item().PaddingTop(10);

            foreach (var (rule, index) in uniqueRules.Select((r, i) => (r, i)))
            {
                var bg = index % 2 == 0 ? "#FAFAFA" : "#FFFFFF";
                var impactColor = ImpactColors.GetValueOrDefault(rule.Impact, "#546E7A");
                var friendlyDesc = RuleDescriptions[rule.RuleId];

                col.Item().Background(bg).BorderBottom(1).BorderColor("#ECEFF1").Padding(8).Row(row =>
                {
                    row.ConstantItem(6).Background(impactColor);
                    row.ConstantItem(8);
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text(text =>
                        {
                            text.Span(rule.RuleId).FontSize(9).Bold().FontColor("#263238");
                            text.Span($"  ×{rule.Count} occurrences").FontSize(8).FontColor(impactColor);
                        });
                        inner.Item().PaddingTop(2).Text(friendlyDesc)
                            .FontSize(9).FontColor("#546E7A");
                    });
                });
            }
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.PaddingTop(8).BorderTop(1).BorderColor("#CFD8DC").Row(row =>
        {
            row.RelativeItem().Text("WCAG Accessibility Report — debugieman.com")
                .FontSize(8).FontColor("#90A4AE");

            row.ConstantItem(60).AlignRight().Text(text =>
            {
                text.Span("Page ").FontSize(8).FontColor("#90A4AE");
                text.CurrentPageNumber().FontSize(8).FontColor("#90A4AE");
                text.Span(" of ").FontSize(8).FontColor("#90A4AE");
                text.TotalPages().FontSize(8).FontColor("#90A4AE");
            });
        });
    }

    private static Dictionary<string, List<AnalysisResultDto>> GroupByImpact(GetAnalysisByIdResult analysis)
    {
        return ImpactOrder.Keys.ToDictionary(
            impact => impact,
            impact => analysis.Results.Where(r => r.Impact == impact).ToList()
        );
    }

    private static string TruncateHtml(string html)
    {
        const int maxLength = 120;
        return html.Length > maxLength ? html[..maxLength] + "..." : html;
    }

    private static byte[]? TryLoadLogo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("DEBUGIEMAN_LOGO.png"));

        if (resourceName is null)
            return null;

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
