using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Market.UI.Controls;

/// <summary>
/// Converte um booleano em Visibility invertido: <c>true</c> → Collapsed, <c>false</c>/nulo → Visible.
/// Usado para exibir mensagens de "empty state" quando um <c>DataGrid.HasItems</c> é falso.
/// </summary>
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility.Visible;
}
