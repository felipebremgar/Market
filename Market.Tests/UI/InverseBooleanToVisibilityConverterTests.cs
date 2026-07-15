using System.Globalization;
using System.Windows;
using Market.UI.Controls;

namespace Market.Tests.UI;

/// <summary>
/// Cobre o conversor que aciona os empty states (v1.4): grid sem itens (false) mostra
/// a mensagem; com itens (true) a esconde.
/// </summary>
public class InverseBooleanToVisibilityConverterTests
{
    private readonly InverseBooleanToVisibilityConverter _conv = new();

    [Fact]
    public void True_vira_Collapsed()
        => Assert.Equal(Visibility.Collapsed, _conv.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture));

    [Fact]
    public void False_vira_Visible()
        => Assert.Equal(Visibility.Visible, _conv.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture));

    [Fact]
    public void Nulo_vira_Visible()
        => Assert.Equal(Visibility.Visible, _conv.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture));
}
