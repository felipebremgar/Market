using System.Globalization;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Market.Application.Services;
using Market.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Market.UI.Views;

public partial class CadastroMercadoriaView : UserControl
{
    private readonly MercadoriaService _servico;
    private readonly IMercadoriaRepository _repositorio;
    private readonly ILogger<CadastroMercadoriaView> _logger;

    public CadastroMercadoriaView(
        MercadoriaService servico,
        IMercadoriaRepository repositorio,
        ILogger<CadastroMercadoriaView> logger)
    {
        InitializeComponent();
        _servico = servico;
        _repositorio = repositorio;
        _logger = logger;

        // Foco inicial no campo de código de barras (fluxo com leitor).
        Loaded += (_, _) => TxtCodigoBarras.Focus();
    }

    // ----- Filtros de entrada -----

    private void Decimal_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = (TextBox)sender;
        foreach (var c in e.Text)
        {
            if (char.IsDigit(c)) continue;
            if (c == ',' && !textBox.Text.Contains(',')) continue;
            e.Handled = true;
            return;
        }
    }

    private void Inteiro_PreviewTextInput(object sender, TextCompositionEventArgs e)
        => e.Handled = !e.Text.All(char.IsDigit);

    // Colagem (Ctrl+V) não passa por PreviewTextInput — filtrada aqui.
    private void Decimal_Pasting(object sender, System.Windows.DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(System.Windows.DataFormats.UnicodeText)
            && e.DataObject.GetData(System.Windows.DataFormats.UnicodeText) is string texto
            && texto.All(c => char.IsDigit(c) || c is ',' or '.'))
            return;
        e.CancelCommand();
    }

    private void Inteiro_Pasting(object sender, System.Windows.DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(System.Windows.DataFormats.UnicodeText)
            && e.DataObject.GetData(System.Windows.DataFormats.UnicodeText) is string texto
            && texto.All(char.IsDigit))
            return;
        e.CancelCommand();
    }

    private void ChkPossuiValidade_Changed(object sender, System.Windows.RoutedEventArgs e)
    {
        DtValidade.IsEnabled = ChkPossuiValidade.IsChecked == true;
        if (!DtValidade.IsEnabled) DtValidade.SelectedDate = null;
    }

    // ----- Leitor de código de barras -----

    private async void TxtCodigoBarras_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        // Consome o Enter para que a leitura não dispare o botão Salvar (IsDefault).
        e.Handled = true;

        var codigo = TxtCodigoBarras.Text.Trim();
        if (codigo.Length == 0)
        {
            TxtNome.Focus();
            return;
        }

        // async void: qualquer exceção não tratada aqui derrubaria o app.
        try
        {
            if (await _repositorio.CodigoBarrasExisteAsync(codigo))
                MostrarAviso($"Já existe uma mercadoria com o código de barras {codigo}.");
            else
                LimparMensagem();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao verificar código de barras {Codigo}.", codigo);
            MostrarAviso("Não foi possível verificar o código de barras agora.");
        }

        TxtNome.Focus();
    }

    // ----- Ações -----

    private async void BtnSalvar_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (ChkPossuiValidade.IsChecked == true && DtValidade.SelectedDate is null)
        {
            MostrarErro("Informe a data de validade ou desmarque \"Possui validade\".");
            return;
        }

        // Parse dos campos numéricos: formato inválido vira erro visível — nunca é
        // convertido silenciosamente para 0 (evita salvar preço "abc" como R$ 0,00).
        var errosFormato = new List<string>();
        TryParseReais(TxtPrecoCusto.Text, "O preço de custo", errosFormato, out var custoReais);
        TryParseReais(TxtPrecoVenda.Text, "O preço de venda", errosFormato, out var vendaReais);
        TryParseInteiro(TxtQuantidade.Text, "A quantidade", errosFormato, out var quantidade);

        if (errosFormato.Count > 0)
        {
            MostrarErro(string.Join(Environment.NewLine, errosFormato));
            return;
        }

        var dados = new CadastroMercadoriaDados
        {
            Nome = TxtNome.Text,
            Fornecedor = TxtFornecedor.Text,
            PrecoCustoReais = custoReais,
            PrecoVendaReais = vendaReais,
            Quantidade = quantidade,
            CodigoBarras = TxtCodigoBarras.Text,
            Validade = ChkPossuiValidade.IsChecked == true && DtValidade.SelectedDate is DateTime data
                ? DateOnly.FromDateTime(data)
                : null
        };

        BtnSalvar.IsEnabled = false;
        try
        {
            var resultado = await _servico.CadastrarAsync(dados);
            if (resultado.Sucesso)
            {
                MostrarSucesso("Mercadoria cadastrada com sucesso.");
                LimparCampos();
            }
            else
            {
                MostrarErro(resultado.MensagemErro);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao cadastrar mercadoria.");
            MostrarErro("Ocorreu um erro inesperado ao salvar. Tente novamente.");
        }
        finally
        {
            BtnSalvar.IsEnabled = true;
        }
    }

    private void BtnLimpar_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        LimparCampos();
        LimparMensagem();
    }

    // ----- Auxiliares -----

    // Limite defensivo (R$): impede overflow na conversão para centavos e valores absurdos.
    private const decimal MaxReais = 9_999_999m;

    private static bool TryParseReais(string texto, string rotulo, List<string> erros, out decimal reais)
    {
        reais = 0m;
        texto = texto.Trim();
        if (texto.Length == 0) return true; // vazio = 0 (mesmo default do DDL)

        if (!decimal.TryParse(texto, NumberStyles.Number, CultureInfo.CurrentCulture, out reais))
        {
            erros.Add($"{rotulo} está em formato inválido.");
            reais = 0m;
            return false;
        }
        if (reais > MaxReais)
        {
            erros.Add($"{rotulo} excede o máximo permitido.");
            reais = 0m;
            return false;
        }
        return true;
    }

    private static bool TryParseInteiro(string texto, string rotulo, List<string> erros, out int valor)
    {
        valor = 0;
        texto = texto.Trim();
        if (texto.Length == 0) return true;

        if (!int.TryParse(texto, NumberStyles.Integer, CultureInfo.CurrentCulture, out valor))
        {
            erros.Add($"{rotulo} está em formato inválido.");
            valor = 0;
            return false;
        }
        return true;
    }

    private void LimparCampos()
    {
        TxtCodigoBarras.Clear();
        TxtNome.Clear();
        TxtFornecedor.Clear();
        TxtPrecoCusto.Clear();
        TxtPrecoVenda.Clear();
        TxtQuantidade.Text = "0";
        ChkPossuiValidade.IsChecked = false;
        DtValidade.SelectedDate = null;
        TxtCodigoBarras.Focus();
    }

    private void MostrarSucesso(string mensagem)
        => Mensagem(mensagem, Color.FromRgb(0xE7, 0xF5, 0xE9), Color.FromRgb(0x2E, 0x7D, 0x32));

    private void MostrarErro(string mensagem)
        => Mensagem(mensagem, Color.FromRgb(0xFD, 0xEC, 0xEA), Color.FromRgb(0xC6, 0x28, 0x28));

    private void MostrarAviso(string mensagem)
        => Mensagem(mensagem, Color.FromRgb(0xFF, 0xF4, 0xE5), Color.FromRgb(0xE6, 0x51, 0x00));

    private void Mensagem(string mensagem, Color fundo, Color texto)
    {
        PainelMensagem.Background = new SolidColorBrush(fundo);
        TxtMensagem.Foreground = new SolidColorBrush(texto);
        TxtMensagem.Text = mensagem;
        PainelMensagem.Visibility = System.Windows.Visibility.Visible;
    }

    private void LimparMensagem()
    {
        TxtMensagem.Text = string.Empty;
        PainelMensagem.Visibility = System.Windows.Visibility.Collapsed;
    }
}
