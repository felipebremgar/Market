using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Market.UI.Controls;

/// <summary>
/// Banner de notificação reutilizável, com variantes Sucesso/Aviso/Erro e auto-dismiss
/// opcional. Substitui os painéis de mensagem ad-hoc espalhados pelas telas.
/// </summary>
public partial class NotificationBanner : UserControl
{
    private readonly DispatcherTimer _timer;

    public NotificationBanner()
    {
        InitializeComponent();
        Visibility = Visibility.Collapsed; // sem mensagem, o controle não ocupa espaço (nem a margem)
        _timer = new DispatcherTimer();
        _timer.Tick += (_, _) => Limpar();
    }

    public void Sucesso(string mensagem, bool autoDismiss = false) => Mostrar(TipoNotificacao.Sucesso, mensagem, autoDismiss);
    public void Aviso(string mensagem, bool autoDismiss = false) => Mostrar(TipoNotificacao.Aviso, mensagem, autoDismiss);
    public void Erro(string mensagem, bool autoDismiss = false) => Mostrar(TipoNotificacao.Erro, mensagem, autoDismiss);

    public void Mostrar(TipoNotificacao tipo, string mensagem, bool autoDismiss = false)
    {
        var tema = NotificacaoTema.Para(tipo);
        Painel.Background = new SolidColorBrush(tema.Fundo);
        Texto.Foreground = new SolidColorBrush(tema.Texto);
        Icone.Foreground = new SolidColorBrush(tema.Texto);
        Icone.Text = tema.Icone;
        Texto.Text = mensagem;
        Visibility = Visibility.Visible;

        _timer.Stop();
        if (autoDismiss)
        {
            _timer.Interval = TimeSpan.FromSeconds(NotificacaoTema.SegundosAutoDismiss);
            _timer.Start();
        }
    }

    public void Limpar()
    {
        _timer.Stop();
        Texto.Text = string.Empty;
        Visibility = Visibility.Collapsed;
    }
}
