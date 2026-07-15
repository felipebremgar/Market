using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Market.Application.Services;
using Market.Domain;
using Market.UI.Controls;
using Microsoft.Extensions.Logging;

namespace Market.UI.Views;

public partial class EditarMercadoriaWindow : Window
{
    private readonly MercadoriaService _servico;
    private readonly ILogger<EditarMercadoriaWindow> _logger;

    private int _id;

    /// <summary>True quando a edição foi salva com sucesso (para o chamador recarregar).</summary>
    public bool Salvou { get; private set; }

    public EditarMercadoriaWindow(MercadoriaService servico, ILogger<EditarMercadoriaWindow> logger)
    {
        InitializeComponent();
        _servico = servico;
        _logger = logger;

        // Selecionar tudo ao focar evita o "0" inicial ficar na frente do número digitado.
        EntradaNumerica.SelecionarTudoAoFocar(TxtQuantidade, TxtPrecoCusto, TxtPrecoVenda);
    }

    /// <summary>Pré-preenche o formulário com os dados da mercadoria a editar.</summary>
    public void Carregar(Mercadoria m)
    {
        _id = m.Id;
        TxtCabecalho.Text = $"Editando: {m.Nome}";
        TxtCodigoBarras.Text = m.CodigoBarras ?? string.Empty;
        TxtNome.Text = m.Nome;
        TxtFornecedor.Text = m.Fornecedor ?? string.Empty;

        // Define a unidade antes dos demais campos: dispara Unidade_Changed, que ajusta a tela.
        if (m.Unidade == UnidadeMedida.Quilo) OpQuilo.IsChecked = true;
        else OpUnidade.IsChecked = true;

        TxtPrecoCusto.Text = Moeda.ParaReais(m.PrecoCusto).ToString("0.00");
        TxtPrecoVenda.Text = Moeda.ParaReais(m.PrecoVenda).ToString("0.00");
        TxtQuantidade.Text = m.Quantidade.ToString();

        if (m.Validade is { } validade)
        {
            ChkPossuiValidade.IsChecked = true;
            DtValidade.SelectedDate = validade.ToDateTime(TimeOnly.MinValue);
        }

        TxtCadastro.Text = $"Cadastrado em {m.DataCadastro:dd/MM/yyyy}  ·  Id {m.Id}";
    }

    private void ChkPossuiValidade_Changed(object sender, RoutedEventArgs e)
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

    private async void BtnSalvar_Click(object sender, RoutedEventArgs e)
    {
        if (ChkPossuiValidade.IsChecked == true && DtValidade.SelectedDate is null)
        {
            MostrarErro("Informe a data de validade ou desmarque \"Possui validade\".");
            return;
        }

        var errosFormato = new List<string>();
        EntradaNumerica.TryParseReais(TxtPrecoCusto.Text, "O preço de custo", errosFormato, out var custoReais);
        EntradaNumerica.TryParseReais(TxtPrecoVenda.Text, "O preço de venda", errosFormato, out var vendaReais);
        EntradaNumerica.TryParseInteiro(TxtQuantidade.Text, "A quantidade", errosFormato, out var quantidade);

        if (errosFormato.Count > 0)
        {
            MostrarErro(string.Join(Environment.NewLine, errosFormato));
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
                var resultado = await _servico.AtualizarAsync(_id, dados);
                if (resultado.Sucesso)
                {
                    Salvou = true;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MostrarErro(resultado.MensagemErro);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao editar mercadoria {Id}.", _id);
            MostrarErro("Ocorreu um erro inesperado ao salvar. Tente novamente.");
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) => Close();

    // Filtros de entrada compartilhados.
    private void Decimal_PreviewTextInput(object sender, TextCompositionEventArgs e)
        => EntradaNumerica.FiltrarDecimal(sender, e);
    private void Inteiro_PreviewTextInput(object sender, TextCompositionEventArgs e)
        => EntradaNumerica.FiltrarInteiro(sender, e);
    private void Decimal_Pasting(object sender, DataObjectPastingEventArgs e)
        => EntradaNumerica.ColarDecimal(sender, e);
    private void Inteiro_Pasting(object sender, DataObjectPastingEventArgs e)
        => EntradaNumerica.ColarInteiro(sender, e);

    private void MostrarErro(string mensagem) => Notificacao.Erro(mensagem);
}
