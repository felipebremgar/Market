using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Market.UI.Controls;

/// <summary>
/// Gráfico de barras simples desenhado com formas WPF (sem dependências externas).
/// Barras positivas em verde e negativas em vermelho, com linha de base no zero.
/// </summary>
public partial class GraficoBarras : UserControl
{
    private static readonly Brush Positiva = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
    private static readonly Brush Negativa = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));
    private static readonly Brush Eixo = new SolidColorBrush(Color.FromRgb(0xCB, 0xD2, 0xDA));

    private IReadOnlyList<(string Rotulo, double Valor)> _dados = System.Array.Empty<(string, double)>();

    public GraficoBarras()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Redesenhar();
    }

    public void Plotar(IReadOnlyList<(string Rotulo, double Valor)> dados)
    {
        _dados = dados ?? System.Array.Empty<(string, double)>();
        Redesenhar();
    }

    private void Redesenhar()
    {
        Area.Children.Clear();

        var largura = ActualWidth;
        var altura = ActualHeight;
        if (_dados.Count == 0 || largura <= 4 || altura <= 8) return;

        var alturaRotulos = _dados.Count <= 12 ? 18.0 : 0.0;
        var alturaPlot = altura - alturaRotulos;

        var maximo = System.Math.Max(_dados.Max(d => d.Valor), 0);
        var minimo = System.Math.Min(_dados.Min(d => d.Valor), 0);
        var amplitude = System.Math.Max(maximo - minimo, 1);

        var yZero = alturaPlot * (maximo / amplitude);

        // Linha de base (zero).
        var baseLinha = new Line
        {
            X1 = 0, X2 = largura, Y1 = yZero, Y2 = yZero,
            Stroke = Eixo, StrokeThickness = 1
        };
        Area.Children.Add(baseLinha);

        var larguraBarra = largura / _dados.Count;
        var larguraInterna = System.Math.Max(larguraBarra * 0.6, 1);

        for (var i = 0; i < _dados.Count; i++)
        {
            var (rotulo, valor) = _dados[i];
            var x = i * larguraBarra + (larguraBarra - larguraInterna) / 2;
            var yValor = alturaPlot * ((maximo - valor) / amplitude);
            var topo = System.Math.Min(yValor, yZero);
            var alturaBarra = System.Math.Max(System.Math.Abs(yValor - yZero), 1);

            var barra = new Rectangle
            {
                Width = larguraInterna,
                Height = alturaBarra,
                Fill = valor >= 0 ? Positiva : Negativa,
                RadiusX = 2, RadiusY = 2
            };
            Canvas.SetLeft(barra, x);
            Canvas.SetTop(barra, topo);
            Area.Children.Add(barra);

            if (alturaRotulos > 0)
            {
                var texto = new TextBlock
                {
                    Text = rotulo,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0x56, 0x6A)),
                    Width = larguraBarra,
                    TextAlignment = TextAlignment.Center
                };
                Canvas.SetLeft(texto, i * larguraBarra);
                Canvas.SetTop(texto, alturaPlot + 2);
                Area.Children.Add(texto);
            }
        }
    }
}
