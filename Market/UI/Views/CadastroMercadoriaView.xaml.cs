using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Market.Application.Services;
using Market.Domain;
using Market.Domain.Repositories;
using Market.UI.Controls;
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

        // Selecionar tudo ao focar evita o "0" inicial ficar na frente do número digitado.
        EntradaNumerica.SelecionarTudoAoFocar(TxtQuantidade, TxtPrecoCusto, TxtPrecoVenda, TxtMargem);
        // Validade de produto novo não pode ser no passado.
        DtValidade.DisplayDateStart = DateTime.Today;

        // Foco inicial no campo de código de barras (fluxo com leitor).
        Loaded += (_, _) => TxtCodigoBarras.Focus();
    }

    // ----- Filtros de entrada (delegam ao utilitário compartilhado) -----

    private void Decimal_PreviewTextInput(object sender, TextCompositionEventArgs e)
        => EntradaNumerica.FiltrarDecimal(sender, e);

    private void Inteiro_PreviewTextInput(object sender, TextCompositionEventArgs e)
        => EntradaNumerica.FiltrarInteiro(sender, e);

    private void Decimal_Pasting(object sender, System.Windows.DataObjectPastingEventArgs e)
        => EntradaNumerica.ColarDecimal(sender, e);

    private void Inteiro_Pasting(object sender, System.Windows.DataObjectPastingEventArgs e)
        => EntradaNumerica.ColarInteiro(sender, e);

    private void ChkPossuiValidade_Changed(object sender, System.Windows.RoutedEventArgs e)
    {
        DtValidade.IsEnabled = ChkPossuiValidade.IsChecked == true;
        if (!DtValidade.IsEnabled) DtValidade.SelectedDate = null;
    }

    /// <summary>Item por peso (verduras/frutas): preços por kg, sem estoque nem validade.</summary>
    private bool PorPeso => OpQuilo.IsChecked == true;

    private void Unidade_Changed(object sender, RoutedEventArgs e)
    {
        // O Checked dispara durante o InitializeComponent, antes dos painéis existirem.
        if (PainelQuantidade is null) return;

        var porPeso = PorPeso;
        PainelQuantidade.Visibility = porPeso ? Visibility.Collapsed : Visibility.Visible;
        PainelValidade.Visibility = porPeso ? Visibility.Collapsed : Visibility.Visible;

        RotuloCusto.Text = porPeso ? "Preço de custo (R$ por kg)" : "Preço de custo (R$)";
        RotuloVenda.Text = porPeso ? "Preço de venda (R$ por kg)" : "Preço de venda (R$)";

        if (porPeso)
        {
            TxtQuantidade.Text = "0";
            ChkPossuiValidade.IsChecked = false;
        }
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

        if (ChkPossuiValidade.IsChecked == true && DtValidade.SelectedDate is DateTime validade
            && validade.Date < DateTime.Today)
        {
            MostrarErro("A validade não pode ser uma data no passado.");
            return;
        }

        // Parse dos campos numéricos: formato inválido vira erro visível — nunca é
        // convertido silenciosamente para 0 (evita salvar preço "abc" como R$ 0,00).
        var errosFormato = new List<string>();
        EntradaNumerica.TryParseReais(TxtPrecoCusto.Text, "O preço de custo", errosFormato, out var custoReais);
        EntradaNumerica.TryParseReais(TxtPrecoVenda.Text, "O preço de venda", errosFormato, out var vendaReais);
        EntradaNumerica.TryParseInteiro(TxtQuantidade.Text, "A quantidade", errosFormato, out var quantidade);

        if (errosFormato.Count > 0)
        {
            MostrarErro(string.Join(Environment.NewLine, errosFormato));
            return;
        }

        // Margem negativa (venda abaixo do custo): avisa e pede confirmação — não bloqueia.
        if (custoReais > 0 && vendaReais < custoReais)
        {
            var confirmar = MessageBox.Show(
                $"O preço de venda ({vendaReais:C}) está abaixo do custo ({custoReais:C}) — margem negativa.\n\nDeseja salvar assim mesmo?",
                "Margem negativa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirmar != MessageBoxResult.Yes)
                return;
        }

        var dados = new CadastroMercadoriaDados
        {
            Nome = TxtNome.Text,
            Fornecedor = TxtFornecedor.Text,
            Unidade = PorPeso ? UnidadeMedida.Quilo : UnidadeMedida.Unidade,
            PrecoCustoReais = custoReais,
            PrecoVendaReais = vendaReais,
            Quantidade = quantidade,
            CodigoBarras = TxtCodigoBarras.Text,
            Validade = ChkPossuiValidade.IsChecked == true && DtValidade.SelectedDate is DateTime data
                ? DateOnly.FromDateTime(data)
                : null
        };

        try
        {
            await BotaoOcupado.ExecutarAsync(BtnSalvar, "Salvando…", async () =>
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
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao cadastrar mercadoria.");
            MostrarErro("Ocorreu um erro inesperado ao salvar. Tente novamente.");
        }
    }

    private void BtnLimpar_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        LimparCampos();
        LimparMensagem();
    }

    // ----- Precificação -----

    private void BtnSugerir_Click(object sender, RoutedEventArgs e)
    {
        var custo = EntradaNumerica.ParseReaisOpcional(TxtPrecoCusto.Text);
        if (custo is not decimal c || c <= 0)
        {
            MostrarAviso("Informe o preço de custo para sugerir o preço de venda.");
            return;
        }

        var margem = EntradaNumerica.ParseInteiroOpcional(TxtMargem.Text) ?? 0;
        var vendaCentavos = Precificacao.PrecoSugeridoCentavos(Moeda.ParaCentavos(c), margem);
        TxtPrecoVenda.Text = Moeda.ParaReais(vendaCentavos).ToString("0.00"); // TextChanged recalcula a margem
    }

    private void Preco_TextChanged(object sender, TextChangedEventArgs e) => AtualizarMargemAtual();

    private void AtualizarMargemAtual()
    {
        // TextChanged pode disparar durante o InitializeComponent, antes de TxtMargemAtual existir.
        if (TxtMargemAtual is null) return;

        var custo = EntradaNumerica.ParseReaisOpcional(TxtPrecoCusto.Text);
        var venda = EntradaNumerica.ParseReaisOpcional(TxtPrecoVenda.Text);
        if (custo is not decimal c || venda is not decimal v || c <= 0)
        {
            TxtMargemAtual.Text = string.Empty;
            return;
        }

        var margem = Precificacao.MargemPercent(Moeda.ParaCentavos(c), Moeda.ParaCentavos(v));
        if (margem is null) { TxtMargemAtual.Text = string.Empty; return; }

        var negativa = v < c;
        TxtMargemAtual.Text = negativa ? $"⚠ Margem negativa: {margem:0.#}%" : $"Margem: {margem:0.#}%";
        TxtMargemAtual.Foreground = new SolidColorBrush(negativa
            ? Color.FromRgb(0xC6, 0x28, 0x28)
            : Color.FromRgb(0x2E, 0x7D, 0x32));
    }

    // ----- Auxiliares -----

    private void LimparCampos()
    {
        TxtCodigoBarras.Clear();
        TxtNome.Clear();
        TxtFornecedor.Clear();
        TxtPrecoCusto.Clear();
        TxtPrecoVenda.Clear();
        TxtQuantidade.Text = "0";
        TxtMargem.Text = "25";
        ChkPossuiValidade.IsChecked = false;
        DtValidade.SelectedDate = null;
        OpUnidade.IsChecked = true;   // volta ao padrão (dispara Unidade_Changed)
        TxtCodigoBarras.Focus();
    }

    private void MostrarSucesso(string mensagem) => Notificacao.Sucesso(mensagem, autoDismiss: true);

    private void MostrarErro(string mensagem) => Notificacao.Erro(mensagem);

    private void MostrarAviso(string mensagem) => Notificacao.Aviso(mensagem);

    private void LimparMensagem() => Notificacao.Limpar();
}
