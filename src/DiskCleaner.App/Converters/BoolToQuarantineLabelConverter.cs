using System.Globalization;
using System.Windows.Data;

namespace DiskCleaner.App.Converters;

public sealed class BoolToQuarantineLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "Quarantined" : "Quarantine";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
