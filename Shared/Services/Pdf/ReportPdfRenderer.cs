using GestionCommerciale.Shared.Models.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace GestionCommerciale.Shared.Services.Pdf;

public static class ReportPdfRenderer
{
    private static readonly CultureInfo CultureFr = CultureInfo.GetCultureInfo("fr-FR");
    private const string TextPrimary = "#111827";
    private const string TextMuted = "#6B7280";
    private const string TableHeaderBg = "#E5E7EB";
    private const string TableBorder = "#D1D5DB";
    private const string TableRowAlt = "#F9FAFB";
    private const string TableDetailBg = "#E8F1FB";
    private const string TableDetailBgAlt = "#DCEAF8";
    private const string TextDetail = "#1E3A5F";
    private const float HeaderLogoWidth = 128f;
    private const float HeaderLogoHeight = 78f;

    public static byte[] Render(string societeNom, ReportPdfModel model, byte[]? logoBytes)
    {
        var rtl = model.IsRightToLeft;
        var columns = rtl ? model.Columns.Reverse().ToList() : model.Columns.ToList();
        var rows = rtl
            ? model.Rows.Select(r => new ReportPdfRow
            {
                Cells = r.Cells.Reverse().ToList(),
                IsDetail = r.IsDetail
            }).ToList()
            : model.Rows.ToList();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(model.Landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
                page.MarginHorizontal(28);
                page.MarginVertical(24);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(TextPrimary));

                page.Header().Column(header =>
                {
                    header.Spacing(6);
                    header.Item().Row(row =>
                    {
                        if (rtl)
                        {
                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().AlignRight().Text(societeNom).Bold().FontSize(14);
                                col.Item().AlignRight().Text(model.Title.ToUpperInvariant()).Bold().FontSize(15);
                                col.Item().AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureFr))
                                    .FontSize(8.5f).FontColor(TextMuted);
                            });
                            if (logoBytes is { Length: > 0 })
                                row.ConstantItem(HeaderLogoWidth).Height(HeaderLogoHeight).Image(logoBytes).FitArea();
                        }
                        else
                        {
                            if (logoBytes is { Length: > 0 })
                                row.ConstantItem(HeaderLogoWidth).Height(HeaderLogoHeight).Image(logoBytes).FitArea();
                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().Text(societeNom).Bold().FontSize(14);
                                col.Item().Text(model.Title.ToUpperInvariant()).Bold().FontSize(15);
                                col.Item().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureFr))
                                    .FontSize(8.5f).FontColor(TextMuted);
                            });
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(model.PeriodLabel))
                    {
                        var period = rtl ? header.Item().AlignRight() : header.Item();
                        period.Text(model.PeriodLabel).FontSize(9).FontColor(TextMuted);
                    }

                    var countLine = rtl ? header.Item().AlignRight() : header.Item();
                    countLine.Text($"{model.Rows.Count} ligne(s)")
                        .FontSize(8.5f).FontColor(TextMuted);
                });

                page.Content().PaddingTop(10).Column(content =>
                {
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(defs =>
                        {
                            foreach (var col in columns)
                                defs.RelativeColumn(Math.Max(0.4f, col.RelativeWidth));
                        });

                        table.Header(header =>
                        {
                            foreach (var col in columns)
                                HeaderCell(header.Cell(), col.Header, CellAlignRight(col.Align, rtl));
                        });

                        var i = 0;
                        foreach (var row in rows)
                        {
                            var bg = row.IsDetail
                                ? (i % 2 == 1 ? TableDetailBgAlt : TableDetailBg)
                                : (i % 2 == 1 ? TableRowAlt : "#FFFFFF");
                            var textColor = row.IsDetail ? TextDetail : TextPrimary;
                            for (var c = 0; c < columns.Count; c++)
                            {
                                var text = c < row.Cells.Count ? row.Cells[c] : string.Empty;
                                BodyCell(
                                    table.Cell().Background(bg),
                                    text,
                                    CellAlignRight(columns[c].Align, rtl),
                                    textColor,
                                    row.IsDetail);
                            }
                            i++;
                        }
                    });

                    if (model.SummaryLines.Count > 0)
                    {
                        content.Item().PaddingTop(14).Border(1).BorderColor(TableBorder)
                            .Background(TableHeaderBg).Padding(10).Column(sum =>
                            {
                                sum.Spacing(4);
                                foreach (var line in model.SummaryLines)
                                {
                                    sum.Item().Row(r =>
                                    {
                                        if (rtl)
                                        {
                                            r.ConstantItem(140).AlignLeft().Text(line.Value).Bold().FontSize(10);
                                            r.RelativeItem().AlignRight().Text(line.Key).FontSize(9).FontColor(TextMuted);
                                        }
                                        else
                                        {
                                            r.RelativeItem().Text(line.Key).FontSize(9).FontColor(TextMuted);
                                            r.ConstantItem(140).AlignRight().Text(line.Value).Bold().FontSize(10);
                                        }
                                    });
                                }
                            });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return doc.GeneratePdf();
    }

    /// <summary>
    /// LTR: Start = left, End = right.
    /// RTL: text columns (Start) align right; numeric (End) stay right-aligned for readability.
    /// </summary>
    private static bool CellAlignRight(PdfTextAlignment align, bool rtl) =>
        rtl || align == PdfTextAlignment.End;

    private static void HeaderCell(IContainer cell, string text, bool alignRight)
    {
        var c = cell.Background(TableHeaderBg).Border(0.5f).BorderColor(TableBorder).Padding(4);
        if (alignRight)
            c.AlignRight().Text(text).Bold().FontSize(8);
        else
            c.Text(text).Bold().FontSize(8);
    }

    private static void BodyCell(IContainer cell, string text, bool alignRight, string textColor, bool isDetail)
    {
        var c = cell.Border(0.5f).BorderColor(TableBorder).Padding(4);
        if (alignRight)
            c = c.AlignRight();
        var styled = c.Text(text).FontSize(isDetail ? 8f : 8.5f).FontColor(textColor);
        if (!isDetail)
            styled.SemiBold();
    }
}
