using System.Windows.Controls;

namespace Market.UI.Controls;

/// <summary>
/// Executa uma ação assíncrona exibindo feedback de "ocupado" no botão que a disparou:
/// desabilita o botão e troca seu texto por um rótulo de processamento durante o await,
/// restaurando o conteúdo e o estado originais ao final (mesmo em caso de exceção).
/// </summary>
public static class BotaoOcupado
{
    public static async Task ExecutarAsync(Button botao, string textoOcupado, Func<Task> acao)
    {
        var conteudoOriginal = botao.Content;
        var habilitadoOriginal = botao.IsEnabled;
        botao.IsEnabled = false;
        botao.Content = textoOcupado;
        try
        {
            await acao();
        }
        finally
        {
            botao.Content = conteudoOriginal;
            botao.IsEnabled = habilitadoOriginal;
        }
    }
}
