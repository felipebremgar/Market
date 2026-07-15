using Market.UI.Controls;

namespace Market.Tests.UI;

/// <summary>
/// Cobre o tema único das notificações (v1.3): cada variante tem cores e ícone próprios
/// e consistentes, garantindo o "estilo único" de Sucesso/Aviso/Erro em toda a aplicação.
/// </summary>
public class NotificacaoTemaTests
{
    [Theory]
    [InlineData(TipoNotificacao.Sucesso)]
    [InlineData(TipoNotificacao.Aviso)]
    [InlineData(TipoNotificacao.Erro)]
    public void Cada_variante_tem_icone_nao_vazio(TipoNotificacao tipo)
    {
        Assert.False(string.IsNullOrWhiteSpace(NotificacaoTema.Para(tipo).Icone));
    }

    [Fact]
    public void Variantes_tem_cores_de_fundo_distintas()
    {
        var sucesso = NotificacaoTema.Para(TipoNotificacao.Sucesso).Fundo;
        var aviso = NotificacaoTema.Para(TipoNotificacao.Aviso).Fundo;
        var erro = NotificacaoTema.Para(TipoNotificacao.Erro).Fundo;

        Assert.NotEqual(sucesso, aviso);
        Assert.NotEqual(sucesso, erro);
        Assert.NotEqual(aviso, erro);
    }

    [Fact]
    public void Erro_usa_vermelho_e_sucesso_usa_verde()
    {
        var erro = NotificacaoTema.Para(TipoNotificacao.Erro).Texto;
        var sucesso = NotificacaoTema.Para(TipoNotificacao.Sucesso).Texto;

        Assert.True(erro.R > erro.G && erro.R > erro.B, "Texto de erro deve ser predominantemente vermelho.");
        Assert.True(sucesso.G > sucesso.R && sucesso.G > sucesso.B, "Texto de sucesso deve ser predominantemente verde.");
    }

    [Fact]
    public void Auto_dismiss_tem_duracao_positiva()
    {
        Assert.True(NotificacaoTema.SegundosAutoDismiss > 0);
    }
}
