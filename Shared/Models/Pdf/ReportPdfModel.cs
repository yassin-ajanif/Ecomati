namespace GestionCommerciale.Shared.Models.Pdf;

public sealed class ReportPdfRow
{
    public required IReadOnlyList<string> Cells { get; init; }
    /// <summary>Nested product / detail line under a parent report row.</summary>
    public bool IsDetail { get; init; }
}

public sealed class ReportPdfModel
{
    public required string Title { get; init; }
    public string? PeriodLabel { get; init; }
    public required IReadOnlyList<PdfTableColumn> Columns { get; init; }
    public required IReadOnlyList<ReportPdfRow> Rows { get; init; }
    public IReadOnlyList<PdfKeyValueLine> SummaryLines { get; init; } = [];
    public bool Landscape { get; init; }
    public bool IsRightToLeft { get; init; }
}
