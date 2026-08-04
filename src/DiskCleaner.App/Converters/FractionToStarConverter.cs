using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DiskCleaner.App.Converters;

/// <summary>
/// Converts a 0..1 fraction into a Star-sized GridLength so used/free proportion
/// bars split correctly regardless of the control's actual pixel width.
/// </summary>
public sealed class FractionToStarConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var fraction = value is double d ? d : 0d;
        return new GridLength(Math.Max(fraction, 0.0001), GridUnitType.Star);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
