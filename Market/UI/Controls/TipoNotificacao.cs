using System.Windows.Media;

namespace Market.UI.Controls;

/// <summary>Variantes visuais de uma notificação exibida ao usuário.</summary>
public enum TipoNotificacao { Sucesso, Aviso, Erro }

/// <summary>Cores de fundo/texto e ícone de uma variante de notificação.</summary>
public readonly record struct TemaNotificacao(Color Fundo, Color Texto, string Icone);

/// <summary>
/// Fonte única do estilo das notificações — garante que Sucesso/Aviso/Erro tenham
/// sempre as mesmas cores e ícones em toda a aplicação.
/// </summary>
public static class NotificacaoTema
{
    /// <summary>Duração padrão do auto-dismiss, em segundos.</summary>
    public const double SegundosAutoDismiss = 4.0;

    public static TemaNotificacao Para(TipoNotificacao tipo) => tipo switch
    {
        TipoNotificacao.Sucesso => new(Color.FromRgb(0xE7, 0xF5, 0xE9), Color.FromRgb(0x1E, 0x7D, 0x32), "✔"),
        TipoNotificacao.Aviso   => new(Color.FromRgb(0xFF, 0xF4, 0xE5), Color.FromRgb(0xE6, 0x51, 0x00), "⚠"),
        TipoNotificacao.Erro    => new(Color.FromRgb(0xFD, 0xEC, 0xEA), Color.FromRgb(0xC6, 0x28, 0x28), "✕"),
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Variante de notificação desconhecida.")
    };
}
