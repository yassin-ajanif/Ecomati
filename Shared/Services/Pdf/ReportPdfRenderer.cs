using GestionCommerciale.Shared.Models.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace GestionCommerciale.Shared.Services.Pdf;

public static class ReportPdfRenderer
{
    private sealed class PdfRenderSegment
    {
        public ReportPdfRow? DayBanner { get; init; }
        public required List<ReportPdfRow> BodyRows { get; init; }
    }

    private static readonly CultureInfo CultureFr = CultureInfo.GetCultureInfo("fr-FR");
    private const string TextPrimary = "#111827";
    private const string TextMuted = "#6B7280";
    private const string TableHeaderBg = "#E5E7EB";
    private const string TableBorder = "#D1D5DB";
    private const string TableRowAlt = "#F9FAFB";
    private const string TableDetailBg = "#E8F1FB";
    private const string TableDetailBgAlt = "#DCEAF8";
    private const string TableDayHeaderBg = "#F3F4F6";
    private const string TextDetail = "#1E3A5F";
    private const string TextDayHeader = "#374151";
    private const float HeaderLogoWidth = 128f;
    private const float HeaderLogoHeight = 78f;

    public static byte[] Render(string societeNom, ReportPdfModel model, byte[]? logoBytes)
    {
        var rtl = model.IsRightToLeft;
        var columns = rtl ? model.Columns.Reverse().ToList() : model.Columns.ToList();
        var rows = rtl
            ? model.Rows.Select(r =>
            {
                var cells = r.Cells.ToList();
                return new ReportPdfRow
                {
                    Cells = cells.AsEnumerable().Reverse().ToList(),
                    CellImages = PadCellImages(cells, r.CellImages)?.AsEnumerable().Reverse().ToList(),
                    IsDetail = r.IsDetail,
                    IsDayHeader = r.IsDayHeader,
                    IsSpacer = r.IsSpacer,
                    IsClientSeparator = r.IsClientSeparator,
                    DayHeaderOn = r.DayHeaderOn,
                    DayHeaderDayName = r.DayHeaderDayName
                };
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

                    var dataRowCount = model.Rows.Count(r =>
                        !r.IsSpacer && !r.IsClientSeparator && !r.IsDayHeader);
                    var countLine = rtl ? header.Item().AlignRight() : header.Item();
                    countLine.Text($"{dataRowCount} ligne(s)")
                        .FontSize(8.5f).FontColor(TextMuted);
                });

                page.Content().PaddingTop(10).Column(content =>
                {
                    var segments = BuildDayGroupedSegments(rows);
                    if (segments is null)
                    {
                        content.Item().Element(c =>
                            RenderDataTable(c, columns, rows, rtl, 0));
                    }
                    else
                    {
                        var stripe = 0;
                        for (var si = 0; si < segments.Count; si++)
                        {
                            var segment = segments[si];
                            if (si > 0)
                                content.Item().PaddingTop(10);

                            if (segment.DayBanner is not null)
                            {
                                content.Item().PaddingBottom(4).Element(c =>
                                    RenderDayBannerAboveTable(c, segment.DayBanner, rtl));
                            }

                            if (segment.BodyRows.Count > 0)
                            {
                                content.Item().Element(c =>
                                {
                                    stripe = RenderDataTable(c, columns, segment.BodyRows, rtl, stripe);
                                });
                            }
                        }
                    }

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

    private const float CellImageSize = 90f;

    /// <summary>Align sparse cell images with cell indices before column reversal (RTL).</summary>
    private static IReadOnlyList<byte[]?>? PadCellImages(
        IReadOnlyList<string> cells,
        IReadOnlyList<byte[]?>? images)
    {
        if (images is null || images.Count == 0)
            return null;

        var padded = new byte[]?[cells.Count];
        for (var i = 0; i < cells.Count; i++)
        {
            if (i < images.Count)
                padded[i] = images[i];
        }
        return padded;
    }

    private static List<PdfRenderSegment>? BuildDayGroupedSegments(IReadOnlyList<ReportPdfRow> rows)
    {
        if (!rows.Any(r => r.IsDayHeader))
            return null;

        var segments = new List<PdfRenderSegment>();
        var preamble = new List<ReportPdfRow>();
        PdfRenderSegment? current = null;

        foreach (var row in rows)
        {
            if (row.IsSpacer)
                continue;

            if (row.IsDayHeader)
            {
                if (current is not null)
                    segments.Add(current);
                current = new PdfRenderSegment { DayBanner = row, BodyRows = [] };
                continue;
            }

            if (current is not null)
                current.BodyRows.Add(row);
            else
                preamble.Add(row);
        }

        if (current is not null)
            segments.Add(current);

        if (preamble.Count > 0)
            segments.Insert(0, new PdfRenderSegment { BodyRows = preamble });

        return segments;
    }

    private static int RenderDataTable(
        IContainer container,
        IReadOnlyList<PdfTableColumn> columns,
        IReadOnlyList<ReportPdfRow> bodyRows,
        bool rtl,
        int stripeIndex)
    {
        container.Table(table =>
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

            foreach (var row in bodyRows)
            {
                if (row.IsClientSeparator)
                {
                    table.Cell().ColumnSpan((uint)columns.Count)
                        .Height(10).Background("#FFFFFF")
                        .BorderBottom(1.5f).BorderColor(TableBorder)
                        .PaddingVertical(4);
                    continue;
                }

                var bg = row.IsDetail
                    ? (stripeIndex % 2 == 1 ? TableDetailBgAlt : TableDetailBg)
                    : (stripeIndex % 2 == 1 ? TableRowAlt : "#FFFFFF");
                var textColor = row.IsDetail ? TextDetail : TextPrimary;
                for (var c = 0; c < columns.Count; c++)
                {
                    var text = c < row.Cells.Count ? row.Cells[c] : string.Empty;
                    byte[]? image = row.CellImages is not null && c < row.CellImages.Count
                        ? row.CellImages[c]
                        : null;
                    BodyCell(
                        table.Cell().Background(bg),
                        text,
                        image,
                        CellAlignRight(columns[c].Align, rtl),
                        textColor,
                        row.IsDetail);
                }
                stripeIndex++;
            }
        });

        return stripeIndex;
    }

    /// <summary>Day label rendered above the table — outside the column grid.</summary>
    private static void RenderDayBannerAboveTable(IContainer container, ReportPdfRow row, bool rtl)
    {
        if (row.DayHeaderOn is null)
            return;

        var summaryParts = row.Cells.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        container
            .PaddingBottom(2)
            .AlignMiddle()
            .Row(banner =>
            {
                if (rtl)
                {
                    banner.RelativeItem().AlignLeft().Element(c =>
                        DaySummaryTotals(c, summaryParts));
                    banner.AutoItem().PaddingLeft(10).Element(c =>
                        DayHeaderLabel(c, row.DayHeaderOn.Value, row.DayHeaderDayName, TextDayHeader));
                }
                else
                {
                    banner.RelativeItem().Element(c =>
                        DayHeaderLabel(c, row.DayHeaderOn.Value, row.DayHeaderDayName, TextDayHeader));
                    banner.AutoItem().AlignRight().Element(c =>
                        DaySummaryTotals(c, summaryParts));
                }
            });
    }

    private static void DayHeaderLabel(
        IContainer cell,
        DateTime date,
        string? dayName,
        string textColor)
    {
        cell.AlignMiddle().AlignLeft().Row(row =>
        {
            row.Spacing(6);
            // Each date segment is its own run so RTL bidi cannot reorder digits.
            row.AutoItem().Row(dateRow =>
            {
                dateRow.Spacing(0);
                void Part(string s)
                {
                    dateRow.AutoItem().Text(s).FontSize(9f).FontColor(textColor).SemiBold();
                }
                Part(date.Day.ToString("00", CultureFr));
                Part("/");
                Part(date.Month.ToString("00", CultureFr));
                Part("/");
                Part(date.Year.ToString("0000", CultureFr));
            });
            if (!string.IsNullOrWhiteSpace(dayName))
            {
                row.AutoItem().Text("—").FontSize(9f).FontColor(TextMuted);
                row.AutoItem().Text(dayName).FontSize(9f).FontColor(textColor).SemiBold();
            }
        });
    }

    private static void DaySummaryTotals(IContainer cell, IReadOnlyList<string> summaryParts)
    {
        cell.AlignMiddle().Row(totalsRow =>
        {
            totalsRow.Spacing(8);
            for (var ti = 0; ti < summaryParts.Count; ti++)
            {
                if (ti > 0)
                    totalsRow.AutoItem().Text("·").FontSize(8.5f).FontColor(TextMuted);
                totalsRow.AutoItem().Text(summaryParts[ti]).FontSize(8.5f).FontColor(TextDayHeader).SemiBold();
            }
        });
    }

    private static void BodyCell(
        IContainer cell,
        string text,
        byte[]? imageBytes,
        bool alignRight,
        string textColor,
        bool isDetail)
    {
        var c = cell.Border(0.5f).BorderColor(TableBorder).Padding(4);
        if (imageBytes is { Length: > 0 })
        {
            c.AlignLeft().Column(col =>
            {
                col.Spacing(2);
                col.Item().Height(CellImageSize).Image(imageBytes).FitArea();
                var styled = col.Item().Text(text).FontSize(isDetail ? 8f : 8.5f).FontColor(textColor);
                if (!isDetail)
                    styled.SemiBold();
            });
            return;
        }

        c = c.AlignMiddle();
        if (alignRight)
            c = c.AlignRight();
        var fontSize = isDetail ? 8f : 8.5f;
        var plain = c.Text(text).FontSize(fontSize).FontColor(textColor);
        if (!isDetail)
            plain.SemiBold();
    }
}
