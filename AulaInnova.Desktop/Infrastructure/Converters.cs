using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace AulaInnova.Desktop.Infrastructure;

public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public bool Inverse { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var flag = value is true;
        if (Inverse)
        {
            flag = !flag;
        }

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class StringToVisibilityConverter : IValueConverter
{
    public bool Inverse { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var hasValue = !string.IsNullOrWhiteSpace(value as string);
        if (Inverse)
        {
            hasValue = !hasValue;
        }

        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class StatusBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = (value as string ?? string.Empty).ToUpperInvariant();
        var key = status switch
        {
            "DANGER" => "DangerBrush",
            "WARNING" => "WarningBrush",
            "SUCCESS" => "SuccessBrush",
            "INFO" => "InfoBrush",
            var s when s.Contains("RECHAZ") || s.Contains("ANUL") || s.Contains("DESHABIL") || s.Contains("SIN COMUNIC") => "DangerBrush",
            var s when s.Contains("PENDIENT") || s.Contains("REVIS") || s.Contains("TARD") || s.Contains("CONFIRMACIÓN") => "WarningBrush",
            var s when s.Contains("ACTIV") || s.Contains("ACEPT") || s.Contains("PUNTUAL") || s.Contains("EN LÍNEA") || s.Contains("REGISTR") => "SuccessBrush",
            _ => "InfoBrush"
        };
        return Application.Current.TryFindResource(key) as Brush ?? Brushes.SlateGray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
