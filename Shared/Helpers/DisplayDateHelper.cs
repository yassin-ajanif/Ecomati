using System.Globalization;

namespace GestionCommerciale.Shared.Helpers;

public static class DisplayDateHelper
{
    public const char LtrMark = '\u200E';

    public static string ShortDate(DateTime date, CultureInfo culture) =>
        date.ToString("dd/MM/yyyy", culture);

    /// <summary>Latin digits for PDF (avoids RTL digit reordering).</summary>
    public static string ShortDatePdf(DateTime date) =>
        date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static string DayName(DateTime date, CultureInfo culture)
    {
        var name = culture.DateTimeFormat.GetDayName(date.DayOfWeek);
        return culture.TextInfo.IsRightToLeft
            ? name
            : culture.TextInfo.ToTitleCase(name);
    }

    public static string DayLabel(DateTime date, CultureInfo culture) =>
        $"{DayName(date, culture)} — {ShortDate(date, culture)}";

    /// <summary>Keep dd/MM/yyyy readable when embedded in Arabic RTL PDF text.</summary>
    public static string DayLabelForRtlPdf(DateTime date, CultureInfo culture)
    {
        var dayName = DayName(date, culture);
        var datePart = ShortDate(date, culture);
        return $"{dayName} — {LtrMark}{datePart}{LtrMark}";
    }

    public static string IsolateLtr(string text) =>
        string.IsNullOrEmpty(text) ? text : $"{LtrMark}{text}{LtrMark}";

    /// <summary>Embed LTR text inside RTL paragraphs (PDF / bidi-safe).</summary>
    public static string EmbedLtr(string text) =>
        string.IsNullOrEmpty(text) ? text : $"\u202A{text}\u202C";
}
