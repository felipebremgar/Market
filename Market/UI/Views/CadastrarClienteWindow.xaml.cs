using System.Windows;
using System.Windows.Media;
using Market.Application.Services;
using Market.Domain;
using Microsoft.Extensions.Logging;

namespace Market.UI.Views;

public partial class CadastrarClienteWindow : Window
{
    private readonly ClienteService _servico;
    private readonly ILogger<CadastrarClienteWindow> _logger;

    /// <summary>True quando o cliente foi cadastrado (para o chamador atualizar/aproveitar).</summary>
    public bool Salvou { get; private set; }

    /// <summary>CPF normalizado do cliente cadastrado (para reuso no PDV — Dia 6).</summary>
    public string? ClienteCpf { get; private set; }

    public CadastrarClienteWindow(ClienteService servico, ILogger<CadastrarClienteWindow> logger)
    {
        InitializeComponent();
        _servico = servico;
        _logger = logger;
        Loaded += (_, _) => TxtCpf.Focus();
    }

    private async void BtnSalvar_Click(object sender, RoutedEventArgs e)
    {
        BtnSalvar.IsEnabled = false;
        try
        {
            var resultado = await _servico.CadastrarAsync(TxtCpf.Text, TxtNome.Text);
            if (resultado.Sucesso)
            {
                Salvou = true;
                ClienteCpf = Cpf.Normalizar(TxtCpf.Text);
                DialogResult = true;
                Close();
            }
            else
            {
                MostrarErro(resultado.MensagemErro);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao cadastrar cliente.");
            MostrarErro("Ocorreu um erro inesperado ao salvar. Tente novamente.");
        }
        finally
        {
            BtnSalvar.IsEnabled = true;
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) => Close();

    private void MostrarErro(string mensagem)
    {
        PainelMensagem.Background = new SolidColorBrush(Color.FromRgb(0xFD, 0xEC, 0xEA));
        TxtMensagem.Foreground = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));
        TxtMensagem.Text = mensagem;
        PainelMensagem.Visibility = Visibility.Visible;
    }
}
