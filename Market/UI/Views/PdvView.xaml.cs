using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Market.Application.Services;
using Market.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Market.UI.Views;

public partial class PdvView : UserControl
{
    private readonly PdvService _pdv;
    private readonly ClienteService _clientes;
    private readonly VendaService _vendas;
    private readonly IServiceProvider _services;
    private readonly ILogger<PdvView> _logger;

    /// <summary>Mínimo de caracteres para disparar a busca incremental de produtos.</summary>
    private const int MinimoParaBuscar = 2;

    private readonly Carrinho _carrinho = new();
    private readonly DispatcherTimer _timerBusca = new() { Interval = TimeSpan.FromMilliseconds(300) };

    private Cliente? _clienteSelecionado;

    /// <summary>CPF do cliente selecionado. Nulo = venda sem cliente.</summary>
    public string? ClienteCpfSelecionado => _clienteSelecionado?.Cpf;

    private sealed record ResultadoBusca(Mercadoria Mercadoria)
    {
        public string Texto => $"{Mercadoria.Nome} — {Moeda.ParaTexto(Mercadoria.PrecoVenda)} (estoque {Mercadoria.Quantidade})";
    }

    public PdvView(
        PdvService pdv, ClienteService clientes, VendaService vendas,
        IServiceProvider services, ILogger<PdvView> logger)
    {
        InitializeComponent();
        _pdv = pdv;
        _clientes = clientes;
        _vendas = vendas;
        _services = services;
        _logger = logger;

        MascaraCpf.Aplicar(TxtCliente);
        // Debounce: só busca quando o caixa para de digitar (evita uma consulta por tecla).
        _timerBusca.Tick += async (_, _) => { _timerBusca.Stop(); await BuscarProdutosAsync(); };

        Loaded += (_, _) => { AtualizarCarrinho(); TxtCodigo.Focus(); };
    }

    // ----- Finalizar -----

    private async void BtnFinalizar_Click(object sender, RoutedEventArgs e)
    {
        if (_carrinho.Vazio) return;

        // Recebimento: forma de pagamento e troco antes de efetivar a venda.
        var recebimento = new RecebimentoWindow(_carrinho.TotalCentavos, ClienteCpfSelecionado is not null)
        { Owner = Window.GetWindow(this) };
        if (recebimento.ShowDialog() != true || recebimento.Pagamento is null)
            return; // caixa voltou; carrinho preservado

        var pagamento = recebimento.Pagamento;

        var conteudoBotao = BtnFinalizar.Content;
        BtnFinalizar.IsEnabled = false;
        BtnFinalizar.Content = "Processando…";
        try
        {
            var resultado = await _vendas.FinalizarVendaAsync(
                ClienteCpfSelecionado, _carrinho.ParaItensCarrinho(), pagamento.Forma, pagamento.DataVencimento);

            if (!resultado.Sucesso)
            {
                // Ex.: estoque mudou entre o bipe e o finalizar — mensagem clara, carrinho preservado.
                MostrarErro(resultado.MensagemErro);
                return;
            }

            var recibo = await _vendas.ObterReciboAsync(resultado.IdGerado!.Value);
            if (recibo is not null)
            {
                var janela = new ReciboWindow(recibo, pagamento) { Owner = Window.GetWindow(this) };
                janela.ShowDialog();
            }

            IniciarNovaVenda();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao finalizar a venda.");
            MostrarErro("Ocorreu um erro inesperado ao finalizar a venda. Tente novamente.");
        }
        finally
        {
            BtnFinalizar.Content = conteudoBotao;
            AtualizarCarrinho(); // reavalia o estado do botão
        }
    }

    private void IniciarNovaVenda()
    {
        _carrinho.Limpar();
        RemoverClienteSelecionado();
        LimparMensagem();
        ListaBusca.Visibility = Visibility.Collapsed;
        AtualizarCarrinho();
        TxtCodigo.Focus();
    }

    // ----- Adicionar itens -----

    private async void TxtCodigo_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        var codigo = TxtCodigo.Text.Trim();
        if (codigo.Length == 0) return;

        try
        {
            var mercadoria = await _pdv.LocalizarPorCodigoAsync(codigo);
            if (mercadoria is null)
                MostrarErro($"Nenhuma mercadoria ativa com o código {codigo}.");
            else
                AdicionarAoCarrinho(mercadoria);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao localizar código {Codigo}.", codigo);
            MostrarErro("Não foi possível buscar o produto agora.");
        }

        TxtCodigo.Clear();
        TxtCodigo.Focus();
    }

    /// <summary>Busca incremental: a cada tecla reinicia o debounce; a lista filtra sozinha.</summary>
    private void TxtBuscaNome_TextChanged(object sender, TextChangedEventArgs e)
    {
        _timerBusca.Stop();

        if (TxtBuscaNome.Text.Trim().Length < MinimoParaBuscar)
        {
            ListaBusca.Visibility = Visibility.Collapsed;
            return;
        }

        _timerBusca.Start();
    }

    private async Task BuscarProdutosAsync()
    {
        var nome = TxtBuscaNome.Text.Trim();
        if (nome.Length < MinimoParaBuscar)
        {
            ListaBusca.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            var achados = await _pdv.BuscarPorNomeAsync(nome);
            ListaBusca.ItemsSource = achados.Select(m => new ResultadoBusca(m)).ToList();
            ListaBusca.DisplayMemberPath = nameof(ResultadoBusca.Texto);
            ListaBusca.Visibility = achados.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha na busca por nome {Nome}.", nome);
            MostrarErro("Não foi possível buscar o produto agora.");
        }
    }

    /// <summary>Teclado no campo de busca: ↓ entra na lista, Enter escolhe, Esc fecha.</summary>
    private void TxtBuscaNome_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            ListaBusca.Visibility = Visibility.Collapsed;
            e.Handled = true;
            return;
        }

        if (ListaBusca.Visibility != Visibility.Visible || ListaBusca.Items.Count == 0) return;

        if (e.Key == Key.Down)
        {
            e.Handled = true;
            ListaBusca.SelectedIndex = 0;
            (ListaBusca.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem)?.Focus();
        }
        else if (e.Key == Key.Enter)
        {
            e.Handled = true;
            if (ListaBusca.SelectedIndex < 0) ListaBusca.SelectedIndex = 0;
            AdicionarSelecionadoDaBusca();
        }
    }

    private void ListaBusca_Escolher(object sender, MouseButtonEventArgs e) => AdicionarSelecionadoDaBusca();

    private void ListaBusca_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { e.Handled = true; AdicionarSelecionadoDaBusca(); }
    }

    private void AdicionarSelecionadoDaBusca()
    {
        if (ListaBusca.SelectedItem is not ResultadoBusca resultado) return;
        AdicionarAoCarrinho(resultado.Mercadoria);
        ListaBusca.Visibility = Visibility.Collapsed;
        TxtBuscaNome.Clear();
        TxtCodigo.Focus();
    }

    private void AdicionarAoCarrinho(Mercadoria mercadoria)
    {
        var resultado = _carrinho.Adicionar(mercadoria);
        if (resultado.Sucesso)
        {
            var linha = _carrinho.Linhas.FirstOrDefault(l => l.MercadoriaId == mercadoria.Id);
            var qtd = linha?.Quantidade ?? 1;
            MostrarSucesso($"Adicionado: {mercadoria.Nome}  (x{qtd})");
        }
        else
        {
            MostrarErro(resultado.MensagemErro);
        }
        AtualizarCarrinho();

        if (resultado.Sucesso)
            DestacarLinha(mercadoria.Id);
    }

    /// <summary>Seleciona e rola até a linha do produto, confirmando visualmente o último bipe.</summary>
    private void DestacarLinha(int mercadoriaId)
    {
        var linha = GridCarrinho.Items.OfType<LinhaCarrinho>()
            .FirstOrDefault(l => l.MercadoriaId == mercadoriaId);
        if (linha is null) return;
        GridCarrinho.SelectedItem = linha;
        GridCarrinho.ScrollIntoView(linha);
    }

    // ----- Quantidade / remoção -----

    private void BtnMais_Click(object sender, RoutedEventArgs e)
    {
        if (LinhaDe(sender) is not { } linha) return;
        var resultado = _carrinho.AlterarQuantidade(linha.MercadoriaId, linha.Quantidade + 1);
        if (!resultado.Sucesso) MostrarErro(resultado.MensagemErro); else LimparMensagem();
        AtualizarCarrinho();
    }

    private void BtnMenos_Click(object sender, RoutedEventArgs e)
    {
        if (LinhaDe(sender) is not { } linha) return;
        if (linha.Quantidade <= 1) return; // use Remover para tirar do carrinho
        _carrinho.AlterarQuantidade(linha.MercadoriaId, linha.Quantidade - 1);
        AtualizarCarrinho();
    }

    private void BtnRemover_Click(object sender, RoutedEventArgs e)
    {
        if (LinhaDe(sender) is not { } linha) return;
        _carrinho.Remover(linha.MercadoriaId);
        AtualizarCarrinho();
    }

    private static LinhaCarrinho? LinhaDe(object sender) => (sender as FrameworkElement)?.DataContext as LinhaCarrinho;

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        if (!_carrinho.Vazio)
        {
            var confirmacao = MessageBox.Show(
                $"Descartar os {_carrinho.Linhas.Count} item(ns) do carrinho?",
                "Cancelar venda", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmacao != MessageBoxResult.Yes)
                return;
        }

        _carrinho.Limpar();
        RemoverClienteSelecionado();
        LimparMensagem();
        AtualizarCarrinho();
        TxtCodigo.Focus();
    }

    // ----- Cliente -----

    /// <summary>Busca do cliente somente por CPF (o campo é mascarado 000.000.000-00).</summary>
    private async void TxtCliente_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        var cpf = Cpf.Normalizar(TxtCliente.Text);
        if (cpf.Length == 0) return;

        if (cpf.Length != 11)
        {
            MostrarErro("Informe o CPF completo (11 dígitos).");
            return;
        }

        try
        {
            var achados = await _clientes.BuscarAsync(cpf, null);
            if (achados.Count == 1)
                SelecionarCliente(achados[0]);
            else
                OferecerCadastroRapido(cpf);   // CPF é a chave: se não existe, oferece cadastrar
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao buscar cliente {Cpf}.", cpf);
            MostrarErro("Não foi possível buscar o cliente agora.");
        }
    }

    private void OferecerCadastroRapido(string cpfSugerido)
    {
        var janela = _services.GetRequiredService<CadastrarClienteWindow>();
        janela.Owner = Window.GetWindow(this);
        if (cpfSugerido.Length > 0) janela.PreencherCpf(cpfSugerido);
        janela.ShowDialog();
        if (janela.Salvou && janela.ClienteCpf is not null)
            _ = SelecionarPorCpfAsync(janela.ClienteCpf);
    }

    /// <summary>Clique no painel do cliente: abre o cadastro para edição e recarrega ao salvar.</summary>
    private void PainelCliente_Click(object sender, MouseButtonEventArgs e)
    {
        if (_clienteSelecionado is null) return;

        var janela = _services.GetRequiredService<CadastrarClienteWindow>();
        janela.Owner = Window.GetWindow(this);
        janela.CarregarParaEdicao(_clienteSelecionado);
        janela.ShowDialog();
        if (janela.Salvou)
            _ = SelecionarPorCpfAsync(_clienteSelecionado.Cpf);
    }

    /// <summary>Recarrega o cliente do banco e o exibe (nome/contato atualizados após edição).</summary>
    private async Task SelecionarPorCpfAsync(string cpf)
    {
        try
        {
            var achados = await _clientes.BuscarAsync(cpf, null);
            if (achados.Count == 1) SelecionarCliente(achados[0]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao recarregar o cliente {Cpf}.", cpf);
        }
    }

    private void SelecionarCliente(Cliente cliente)
    {
        _clienteSelecionado = cliente;
        TxtClienteNome.Text = cliente.Nome;
        TxtClienteCpf.Text = MascaraCpf.Formatar(cliente.Cpf);
        TxtClienteContato.Text = string.IsNullOrWhiteSpace(cliente.Contato)
            ? "sem contato cadastrado"
            : cliente.Contato;
        PainelCliente.Visibility = Visibility.Visible;
        TxtCliente.Clear();
        LimparMensagem();
    }

    private void BtnRemoverCliente_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;   // impede o clique de borbulhar e abrir a edição do cliente
        RemoverClienteSelecionado();
    }

    private void RemoverClienteSelecionado()
    {
        _clienteSelecionado = null;
        PainelCliente.Visibility = Visibility.Collapsed;
    }

    // ----- Render -----

    private void AtualizarCarrinho()
    {
        GridCarrinho.ItemsSource = _carrinho.Linhas.ToList();
        TxtTotal.Text = _carrinho.TotalTexto;
        BtnFinalizar.IsEnabled = !_carrinho.Vazio;
    }

    private void MostrarErro(string mensagem) => Notificacao.Erro(mensagem);

    private void MostrarSucesso(string mensagem) => Notificacao.Sucesso(mensagem, autoDismiss: true);

    private void LimparMensagem() => Notificacao.Limpar();

    // ----- Quantidade digitável (edição direta na coluna Qtd) -----

    private void GridCarrinho_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit) return;
        if (e.Column != ColunaQtd) return;
        if (e.Row.Item is not LinhaCarrinho linha) return;
        if (e.EditingElement is not TextBox tb) return;

        var texto = tb.Text.Trim();
        if (!int.TryParse(texto, out var novaQtd) || novaQtd <= 0)
        {
            MostrarErro("Quantidade inválida. Informe um número inteiro maior que zero.");
            tb.Text = linha.Quantidade.ToString(); // reverte para o valor atual
            return;
        }

        if (novaQtd == linha.Quantidade) { LimparMensagem(); return; }

        var resultado = _carrinho.AlterarQuantidade(linha.MercadoriaId, novaQtd);
        if (!resultado.Sucesso)
        {
            MostrarErro(resultado.MensagemErro);
            tb.Text = linha.Quantidade.ToString();
            return;
        }

        LimparMensagem();
        // Recalcula subtotais/total após o commit da edição (LinhaCarrinho não notifica mudanças).
        Dispatcher.BeginInvoke(new Action(AtualizarCarrinho), DispatcherPriority.Background);
    }
}
