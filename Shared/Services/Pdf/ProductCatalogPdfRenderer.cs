using GestionCommerciale.Modules.Stock.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace GestionCommerciale.Shared.Services.Pdf;

public static class ProductCatalogPdfRenderer
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("fr-FR");
    private const string TextPrimary = "#111827";
    private const string TextMuted = "#6B7280";
    private const string TableHeaderBg = "#E5E7EB";
    private const string TableBorder = "#D1D5DB";
    private const string TableRowAlt = "#F9FAFB";
    private const float HeaderLogoWidth = 128f;
    private const float HeaderLogoHeight = 78f;

    public static byte[] Render(
        string societeNom,
        IReadOnlyList<Produit> products,
        string? searchTerm,
        byte[]? logoBytes)
    {
        var filterLabel = string.IsNullOrWhiteSpace(searchTerm)
            ? null
            : searchTerm.Trim();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.MarginHorizontal(36);
                page.MarginVertical(28);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(TextPrimary));

                page.Header().Column(header =>
                {
                    header.Spacing(8);
                    header.Item().Row(row =>
                    {
                        if (logoBytes is { Length: > 0 })
                            row.ConstantItem(HeaderLogoWidth).Height(HeaderLogoHeight).Image(logoBytes).FitArea();
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text(societeNom).Bold().FontSize(16);
                            col.Item().Text("CATALOGUE PRODUITS").Bold().FontSize(17);
                            col.Item().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm", Culture))
                                .FontSize(9).FontColor(TextMuted);
                        });
                    });
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.Span($"{products.Count} produit(s)").FontSize(9).FontColor(TextMuted);
                            if (filterLabel != null)
                                text.Span($"  —  Filtre : {filterLabel}").FontSize(9).FontColor(TextMuted);
                        });
                    });
                });

                page.Content().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(90);
                        columns.RelativeColumn(3);
                        columns.ConstantColumn(72);
                        columns.ConstantColumn(72);
                    });

                    table.Header(header =>
                    {
                        HeaderCell(header.Cell(), "Réf.");
                        HeaderCell(header.Cell(), "Désignation");
                        HeaderCell(header.Cell(), "Stock", alignRight: true);
                        HeaderCell(header.Cell(), "Min.", alignRight: true);
                    });

                    var i = 0;
                    foreach (var p in products)
                    {
                        var bg = i % 2 == 1 ? TableRowAlt : "#FFFFFF";
                        BodyCell(table.Cell().Background(bg), p.Reference ?? string.Empty);
                        BodyCell(table.Cell().Background(bg), p.Designation ?? string.Empty);
                        BodyCell(table.Cell().Background(bg), Fmt(p.StockActuel), alignRight: true);
                        BodyCell(table.Cell().Background(bg), Fmt(p.StockMinimum), alignRight: true);
                        i++;
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

    private static string Fmt(decimal value) => value.ToString("N2", Culture);

    private static void HeaderCell(IContainer cell, string text, bool alignRight = false)
    {
        var c = cell.Background(TableHeaderBg).Border(0.5f).BorderColor(TableBorder).Padding(5);
        if (alignRight)
            c.AlignRight().Text(text).Bold().FontSize(8.5f);
        else
            c.Text(text).Bold().FontSize(8.5f);
    }

    private static void BodyCell(IContainer cell, string text, bool alignRight = false)
    {
        var c = cell.Border(0.5f).BorderColor(TableBorder).Padding(5);
        if (alignRight)
            c.AlignRight().Text(text).FontSize(8.5f);
        else
            c.Text(text).FontSize(8.5f);
    }
}
