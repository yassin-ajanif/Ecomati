using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace GestionCommerciale.Shared.Converters;

public sealed class PositiveNegativeForegroundConverter : IValueConverter
{
    public static readonly PositiveNegativeForegroundConverter Instance = new();

    private static readonly IBrush Green = new SolidColorBrush(Color.Parse("#16A34A"));
    private static readonly IBrush Red = new SolidColorBrush(Color.Parse("#DC2626"));
    private static readonly IBrush DefaultFg = new SolidColorBrush(Color.Parse("#0F172A"));
    private static readonly IBrush GreenBg = new SolidColorBrush(Color.Parse("#DCFCE7"));
    private static readonly IBrush RedBg = new SolidColorBrush(Color.Parse("#FEE2E2"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        decimal? number = value switch
        {
            decimal d => d,
            double d => (decimal)d,
            float f => (decimal)f,
            int i => i,
            long l => l,
            bool b => b ? 1m : -1m,
            _ => null
        };
        if (number is null)
            return DefaultFg;

        var isPositive = number.Value >= 0;
        var kind = (parameter?.ToString() ?? "fg").Trim().ToLowerInvariant();
        return kind switch
        {
            "bg" => isPositive ? GreenBg : RedBg,
            // Red only when negative; keep readable default otherwise (null would hide text).
            "neg" or "negative" => isPositive ? DefaultFg : Red,
            _ => isPositive ? Green : Red,
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
