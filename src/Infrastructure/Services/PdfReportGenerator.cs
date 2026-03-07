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
            });

            col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#1A237E");
        });
    }

    private static void ComposeContent(IContainer container, GetAnalysisByIdResult analysis, Dictionary<string, List<AnalysisResultDto>> grouped)
    {
        container.PaddingTop(16).Column(col =>
        {
            col.Item().Element(c => ComposeSummary(c, analysis.Results.ToList()));

            col.Item().PaddingTop(20);

            if (!analysis.Results.Any())
            {
                col.Item().Padding(20).AlignCenter()
                    .Text("No accessibility violations found.")
                    .FontSize(13).Bold().FontColor("#388E3C");
                return;
            }

            foreach (var (impact, label) in ImpactOrder)
            {
                if (!grouped.TryGetValue(impact, out var items) || items.Count == 0)
                    continue;

                col.Item().Element(c => ComposeSection(c, label, impact, items));
                col.Item().PaddingTop(16);
            }
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

        container.Column(col =>
        {
            col.Item().Background(color).Padding(8).Row(row =>
            {
                row.RelativeItem().Text($"{label} Issues ({items.Count})")
                    .FontSize(11).Bold().FontColor(Colors.White);
            });

            foreach (var (item, index) in items.Select((v, i) => (v, i)))
            {
                var bg = index % 2 == 0 ? "#FAFAFA" : "#FFFFFF";

                col.Item().Background(bg).BorderBottom(1).BorderColor("#ECEFF1").Padding(10).Column(inner =>
                {
                    inner.Item().Text(item.RuleId).FontSize(10).Bold().FontColor("#263238");

                    inner.Item().PaddingTop(3).Text(item.Description)
                        .FontSize(9).FontColor("#546E7A");

                    if (!string.IsNullOrWhiteSpace(item.HtmlElement))
                    {
                        inner.Item().PaddingTop(5)
                            .Background("#F5F5F5").Padding(6)
                            .Text(TruncateHtml(item.HtmlElement))
                            .FontSize(8).FontColor("#37474F");
                    }

                    if (!string.IsNullOrWhiteSpace(item.HelpUrl))
                    {
                        inner.Item().PaddingTop(4)
                            .Text($"More info: {item.HelpUrl}")
                            .FontSize(8).FontColor("#1565C0").Underline();
                    }
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
