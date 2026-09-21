namespace GestionCommerciale.Shared.Models.Pdf;

public sealed class ReportPdfRow
{
    public required IReadOnlyList<string> Cells { get; init; }
    /// <summary>Optional image bytes aligned with <see cref="Cells"/> by column index.</summary>
    public IReadOnlyList<byte[]?>? CellImages { get; init; }
    /// <summary>Nested product / detail line under a parent report row.</summary>
    public bool IsDetail { get; init; }
    /// <summary>Day / section header inside a grouped report (e.g. sales by client).</summary>
    public bool IsDayHeader { get; init; }
    /// <summary>Visual gap between day groups; renders as a short empty row.</summary>
    public bool IsSpacer { get; init; }
    /// <summary>Full-width horizontal rule between client groups.</summary>
    public bool IsClientSeparator { get; init; }
    /// <summary>Day header: calendar date rendered as separate LTR segments in PDF.</summary>
    public DateTime? DayHeaderOn { get; init; }
    /// <summary>Day header: weekday name on the same line as the date.</summary>
    public string? DayHeaderDayName { get; init; }
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
    /// <summary>Keep LTR layout even when UI language is Arabic (e.g. import calculator table).</summary>
    public bool ForceLeftToRight { get; init; }
}
