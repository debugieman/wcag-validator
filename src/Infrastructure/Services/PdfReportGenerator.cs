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

    private static readonly Dictionary<string, string> FriendlyNames = new()
    {
        ["color-contrast"]                   = "Low Color Contrast",
        ["image-alt"]                        = "Missing Image Alt Text",
        ["svg-image-missing-alt"]            = "Missing SVG Alt Text",
        ["button-name"]                      = "Unlabelled Button",
        ["link-name"]                        = "Unlabelled Link",
        ["input-missing-label"]              = "Form Field Without Label",
        ["select-textarea-missing-label"]    = "Dropdown or Text Area Without Label",
        ["label"]                            = "Missing Form Label",
        ["html-has-lang"]                    = "Missing Page Language",
        ["heading-level-skipped"]            = "Skipped Heading Level",
        ["heading-first-not-h1"]             = "First Heading Is Not H1",
        ["skip-navigation-missing"]          = "Missing Skip Navigation Link",
        ["landmark-one-main"]                = "Missing Main Landmark",
        ["landmark-unique"]                  = "Duplicate Landmark Regions",
        ["region"]                           = "Content Outside Landmark Regions",
        ["list"]                             = "Incorrect List Structure",
        ["table-missing-caption"]            = "Table Without Caption",
        ["aria-allowed-role"]                = "Invalid ARIA Role",
        ["focus-visible-missing"]            = "Invisible Keyboard Focus",
        ["interactive-not-focusable"]        = "Element Not Keyboard Accessible",
        ["keyboard-trap"]                    = "Keyboard Focus Trap",
        ["reflow-horizontal-scroll"]         = "Horizontal Scroll at Small Viewport",
        ["touch-target-too-small"]           = "Touch Target Too Small",
        ["animation-reduced-motion-missing"] = "Animation Ignores Reduced Motion",
    };

    private static readonly Dictionary<string, string> WcagCriteria = new()
    {
        ["color-contrast"]                   = "WCAG 1.4.3",
        ["image-alt"]                        = "WCAG 1.1.1",
        ["svg-image-missing-alt"]            = "WCAG 1.1.1",
        ["button-name"]                      = "WCAG 4.1.2",
        ["link-name"]                        = "WCAG 2.4.4",
        ["input-missing-label"]              = "WCAG 1.3.1",
        ["select-textarea-missing-label"]    = "WCAG 1.3.1",
        ["label"]                            = "WCAG 1.3.1",
        ["html-has-lang"]                    = "WCAG 3.1.1",
        ["heading-level-skipped"]            = "WCAG 1.3.1",
        ["heading-first-not-h1"]             = "WCAG 1.3.1",
        ["skip-navigation-missing"]          = "WCAG 2.4.1",
        ["landmark-one-main"]                = "WCAG 1.3.6",
        ["landmark-unique"]                  = "WCAG 1.3.6",
        ["region"]                           = "WCAG 1.3.6",
        ["list"]                             = "WCAG 1.3.1",
        ["table-missing-caption"]            = "WCAG 1.3.1",
        ["aria-allowed-role"]                = "WCAG 4.1.2",
        ["focus-visible-missing"]            = "WCAG 2.4.7",
        ["interactive-not-focusable"]        = "WCAG 2.1.1",
        ["keyboard-trap"]                    = "WCAG 2.1.2",
        ["reflow-horizontal-scroll"]         = "WCAG 1.4.10",
        ["touch-target-too-small"]           = "WCAG 2.5.5",
        ["animation-reduced-motion-missing"] = "WCAG 2.3.3",
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

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Content().Element(c => ComposeCoverPage(c, logoBytes, analysis));
            });

            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c, logoBytes, analysis));
                page.Content().Element(c => ComposeContent(c, analysis));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    public int CalculateScore(GetAnalysisByIdResult analysis) =>
        CalculateScore(analysis.Results.ToList());

    private static void ComposeCoverPage(IContainer container, byte[]? logoBytes, GetAnalysisByIdResult analysis)
    {
        var allResults = analysis.Results.ToList();
        var score = CalculateScore(allResults);
        var scoreColor = ScoreColor(score);
        var scoreLabel = score >= 80 ? "Good" : score >= 50 ? "Needs Improvement" : "Poor";

        var critical = allResults.Count(r => r.Impact == "critical");
        var serious  = allResults.Count(r => r.Impact == "serious");
        var moderate = allResults.Count(r => r.Impact == "moderate");
        var minor    = allResults.Count(r => r.Impact == "minor");

        var pageGroups = analysis.DeepScan
            ? GroupByPage(allResults)
            : [];

        container.Column(col =>
        {
            col.Item().Height(60);

            if (logoBytes is not null)
                col.Item().AlignCenter().Width(120).Image(logoBytes);

            col.Item().Height(30);

            col.Item().AlignCenter().Text("WCAG Accessibility Report")
                .FontSize(28).Bold().FontColor("#1A237E");

            if (analysis.DeepScan)
                col.Item().PaddingTop(6).AlignCenter()
                    .Background("#E8EAF6").Padding(4)
                    .Text("Deep Scan  ·  Multiple Pages")
                    .FontSize(10).Bold().FontColor("#1A237E");

            col.Item().PaddingTop(6).AlignCenter().Text(analysis.Url)
                .FontSize(11).FontColor("#546E7A");

            col.Item().PaddingTop(4).AlignCenter()
                .Text($"Generated: {analysis.CompletedAt?.ToString("dd MMM yyyy HH:mm") ?? DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")} UTC")
                .FontSize(9).FontColor("#90A4AE");

            col.Item().Height(30);

            col.Item().AlignCenter().Column(scoreCol =>
            {
                var scoreTitle = analysis.DeepScan ? "Average Score" : "Accessibility Score";
                scoreCol.Item().AlignCenter().Text($"{score}").FontSize(72).Bold().FontColor(scoreColor);
                scoreCol.Item().AlignCenter().Text("out of 100").FontSize(12).FontColor("#90A4AE");
                scoreCol.Item().PaddingTop(4).AlignCenter().Text(scoreLabel).FontSize(14).Bold().FontColor(scoreColor);
                scoreCol.Item().AlignCenter().Text(scoreTitle).FontSize(10).FontColor("#90A4AE");
            });

            col.Item().Height(24);

            col.Item().AlignCenter().Width(300).Column(bar =>
            {
                bar.Item().Row(row =>
                {
                    if (score > 0)
                        row.RelativeItem(score).Height(10).Background(scoreColor);
                    if (score < 100)
                        row.RelativeItem(100 - score).Height(10).Background("#ECEFF1");
                });
            });

            col.Item().Height(24);

            col.Item().AlignCenter().Width(400).Row(row =>
            {
                CoverStatBadge(row.RelativeItem(), critical.ToString(), "Critical", "#D32F2F", "#FFEBEE");
                CoverStatBadge(row.RelativeItem(), serious.ToString(),  "Serious",  "#F57C00", "#FFF3E0");
                CoverStatBadge(row.RelativeItem(), moderate.ToString(), "Moderate", "#F9A825", "#FFFDE7");
                CoverStatBadge(row.RelativeItem(), minor.ToString(),    "Minor",    "#388E3C", "#E8F5E9");
            });

            // Per-page score table — only for deep scan
            if (pageGroups.Count > 0)
            {
                col.Item().Height(20);

                col.Item().AlignCenter().Width(460).Column(tableCol =>
                {
                    tableCol.Item().Background("#1A237E").Padding(8)
                        .Text("Score per page").FontSize(10).Bold().FontColor(Colors.White);

                    foreach (var (pageUrl, pageResults) in pageGroups)
                    {
                        var pageScore = CalculateScore(pageResults);
                        var pageColor = ScoreColor(pageScore);
                        var shortUrl = ShortenUrl(pageUrl);

                        tableCol.Item()
                            .BorderBottom(1).BorderColor("#ECEFF1")
                            .Background("#FAFAFA")
                            .Padding(6).Row(row =>
                            {
                                row.RelativeItem().AlignMiddle()
                                    .Text(shortUrl).FontSize(9).FontColor("#263238");

                                row.ConstantItem(50).AlignMiddle().AlignRight()
                                    .Text($"{pageScore}/100").FontSize(9).Bold().FontColor(pageColor);

                                row.ConstantItem(8);

                                row.ConstantItem(80).AlignMiddle().Column(barCol =>
                                {
                                    barCol.Item().Row(barRow =>
                                    {
                                        if (pageScore > 0)
                                            barRow.RelativeItem(pageScore).Height(6).Background(pageColor);
                                        if (pageScore < 100)
                                            barRow.RelativeItem(100 - pageScore).Height(6).Background("#ECEFF1");
                                    });
                                });
                            });
                    }
                });
            }

            col.Item().Extend();

            col.Item().AlignCenter().Text("debugieman.com")
                .FontSize(9).FontColor("#CFD8DC");
        });
    }

    private static void CoverStatBadge(IContainer container, string count, string label, string color, string bg)
    {
        container.Padding(6).Background(bg).Padding(10).Column(col =>
        {
            col.Item().AlignCenter().Text(count).FontSize(18).Bold().FontColor(color);
            col.Item().AlignCenter().Text(label).FontSize(8).FontColor(color);
        });
    }

    private static void ComposeHeader(IContainer container, byte[]? logoBytes, GetAnalysisByIdResult analysis)
    {
        var score = CalculateScore(analysis.Results.ToList());
        var scoreColor = ScoreColor(score);

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

    internal static int CalculateScore(List<AnalysisResultDto> results)
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

    private static void ComposeContent(IContainer container, GetAnalysisByIdResult analysis)
    {
        var allResults = analysis.Results.ToList();

        container.PaddingTop(16).Column(col =>
        {
            col.Item().Element(ComposeIntro);
            col.Item().PaddingTop(16);
            col.Item().Element(c => ComposeSummary(c, allResults));
            col.Item().PaddingTop(20);

            if (allResults.Count == 0)
            {
                col.Item().Padding(20).AlignCenter()
                    .Text("No accessibility violations found.")
                    .FontSize(13).Bold().FontColor("#388E3C");
                return;
            }

            col.Item().Element(c => ComposeTopPriorities(c, allResults));
            col.Item().PaddingTop(20);

            if (analysis.DeepScan)
            {
                foreach (var (pageUrl, pageResults) in GroupByPage(allResults))
                {
                    col.Item().Element(c => ComposePageSection(c, pageUrl, pageResults));
                    col.Item().PaddingTop(20);
                }
            }
            else
            {
                var grouped = GroupByImpact(allResults);
                foreach (var (impact, label) in ImpactOrder)
                {
                    if (!grouped.TryGetValue(impact, out var items) || items.Count == 0)
                        continue;

                    col.Item().Element(c => ComposeSection(c, label, impact, items));
                    col.Item().PaddingTop(16);
                }
            }
        });
    }

    private static void ComposePageSection(IContainer container, string pageUrl, List<AnalysisResultDto> pageResults)
    {
        var score = CalculateScore(pageResults);
        var scoreColor = ScoreColor(score);
        var shortUrl = ShortenUrl(pageUrl);
        var grouped = GroupByImpact(pageResults);

        container.Column(col =>
        {
            // Page header bar
            col.Item().Background("#1A237E").Padding(10).Row(row =>
            {
                row.RelativeItem().AlignMiddle().Column(inner =>
                {
                    inner.Item().Text(shortUrl).FontSize(11).Bold().FontColor(Colors.White);
                    inner.Item().Text(pageUrl).FontSize(8).FontColor("#9FA8DA");
                });

                row.ConstantItem(80).AlignMiddle().AlignRight().Column(scoreCol =>
                {
                    scoreCol.Item().AlignRight()
                        .Text($"{score}/100").FontSize(14).Bold().FontColor(scoreColor == "#388E3C" ? "#A5D6A7" : scoreColor == "#F57C00" ? "#FFCC80" : "#EF9A9A");
                    scoreCol.Item().AlignRight()
                        .Text("page score").FontSize(7).FontColor("#9FA8DA");
                });
            });

            col.Item().PaddingTop(8);

            if (pageResults.Count == 0)
            {
                col.Item().Background("#E8F5E9").Padding(10)
                    .Text("No violations found on this page.")
                    .FontSize(10).FontColor("#388E3C");
                return;
            }

            foreach (var (impact, label) in ImpactOrder)
            {
                if (!grouped.TryGetValue(impact, out var items) || items.Count == 0)
                    continue;

                col.Item().Element(c => ComposeSection(c, label, impact, items));
                col.Item().PaddingTop(10);
            }
        });
    }

    private static void ComposeIntro(IContainer container)
    {
        container.Background("#F8F9FF").Padding(14).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(text =>
                {
                    text.Span("This report was generated by ").FontSize(9).FontColor("#37474F");
                    text.Span("WCAG Analyzer").FontSize(9).Bold().FontColor("#1A237E");
                    text.Span(" and covers accessibility issues found on your website. Issues are grouped by severity. Fix Critical and Serious issues first — they block access for users with disabilities and may expose you to legal risk under the ").FontSize(9).FontColor("#37474F");
                    text.Span("EU Accessibility Act (EAA 2025).").FontSize(9).Bold().FontColor("#D32F2F");
                });
            });

            row.ConstantItem(16);

            row.ConstantItem(220).Row(badgeRow =>
            {
                PriorityBadge(badgeRow.RelativeItem(), "Critical", "#D32F2F", "#FFEBEE", "Fix immediately.");
                PriorityBadge(badgeRow.RelativeItem(), "Serious",  "#F57C00", "#FFF3E0", "Fix before release.");
                PriorityBadge(badgeRow.RelativeItem(), "Moderate", "#F9A825", "#FFFDE7", "Next sprint.");
                PriorityBadge(badgeRow.RelativeItem(), "Minor",    "#388E3C", "#E8F5E9", "When possible.");
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

    internal record GroupedRule(
        string RuleId,
        int Count,
        string Description,
        string? HelpUrl,
        List<string> Examples);

    internal static List<GroupedRule> GroupRules(List<AnalysisResultDto> items) =>
        items
            .GroupBy(r => r.RuleId)
            .Select(g => new GroupedRule(
                RuleId:      g.Key,
                Count:       g.Count(),
                Description: g.First().Description,
                HelpUrl:     g.First().HelpUrl,
                Examples:    g.Where(r => !string.IsNullOrWhiteSpace(r.HtmlElement))
                              .Take(2)
                              .Select(r => r.HtmlElement!)
                              .ToList()
            ))
            .ToList();

    private static void ComposeSection(IContainer container, string label, string impact, List<AnalysisResultDto> items)
    {
        var color = ImpactColors.GetValueOrDefault(impact, "#546E7A");
        var grouped = GroupRules(items);

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
                        row.RelativeItem().Column(nameCol =>
                        {
                            var friendly = FriendlyNames.GetValueOrDefault(group.RuleId);
                            if (friendly is not null)
                                nameCol.Item().Text(friendly).FontSize(10).Bold().FontColor("#263238");
                            nameCol.Item().Text(text =>
                            {
                                text.Span(group.RuleId).FontSize(8).FontColor("#90A4AE");
                                var criterion = WcagCriteria.GetValueOrDefault(group.RuleId);
                                if (criterion is not null)
                                    text.Span($"  ·  {criterion}").FontSize(8).Bold().FontColor("#1565C0");
                            });
                        });
                        row.ConstantItem(80).AlignRight()
                            .Text($"×{group.Count} occurrences")
                            .FontSize(8).FontColor(color);
                    });

                    var displayDesc = RuleDescriptions.GetValueOrDefault(group.RuleId) ?? group.Description;
                    inner.Item().PaddingTop(3).Text(displayDesc)
                        .FontSize(9).FontColor("#546E7A");

                    if (group.Count > 5)
                    {
                        inner.Item().PaddingTop(5)
                            .Background("#FFF8E1").Padding(6)
                            .Text($"⚠ This issue appears {group.Count} times on your page. Focus on fixing the pattern rather than individual elements.")
                            .FontSize(8).FontColor("#F57C00");
                    }
                    else if (group.Examples.Count > 0)
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
                        var friendly = FriendlyNames.GetValueOrDefault(item.RuleId);
                        inner.Item().Text(text =>
                        {
                            text.Span(friendly ?? item.RuleId).FontSize(10).Bold().FontColor("#263238");
                            text.Span($"  ×{item.Count}").FontSize(9).FontColor(impactColor);
                        });
                        if (friendly is not null)
                            inner.Item().Text(item.RuleId).FontSize(8).FontColor("#90A4AE");
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

    private static Dictionary<string, List<AnalysisResultDto>> GroupByImpact(List<AnalysisResultDto> results) =>
        ImpactOrder.Keys.ToDictionary(
            impact => impact,
            impact => results.Where(r => r.Impact == impact).ToList()
        );

    internal static List<KeyValuePair<string, List<AnalysisResultDto>>> GroupByPage(List<AnalysisResultDto> results) =>
        results
            .GroupBy(r => r.PageUrl ?? "")
            .OrderBy(g => g.Key)
            .Select(g => new KeyValuePair<string, List<AnalysisResultDto>>(g.Key, g.ToList()))
            .ToList();

    private static string ScoreColor(int score) =>
        score >= 80 ? "#388E3C" : score >= 50 ? "#F57C00" : "#D32F2F";

    private static string ShortenUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;
        var path = uri.AbsolutePath.TrimEnd('/');
        return string.IsNullOrEmpty(path) ? uri.Host : $"{uri.Host}{path}";
    }

    internal static string TruncateHtml(string html)
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
