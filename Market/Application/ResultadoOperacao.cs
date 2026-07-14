namespace Market.Application;

/// <summary>
/// Resultado padrão de uma operação de negócio: sucesso com Id opcional,
/// ou falha com a lista de mensagens de erro (em português, prontas para exibição).
/// </summary>
public class ResultadoOperacao
{
    public bool Sucesso { get; }
    public IReadOnlyList<string> Erros { get; }
    public int? IdGerado { get; }

    private ResultadoOperacao(bool sucesso, IReadOnlyList<string> erros, int? idGerado)
    {
        Sucesso = sucesso;
        Erros = erros;
        IdGerado = idGerado;
    }

    public string MensagemErro => string.Join(Environment.NewLine, Erros);

    public static ResultadoOperacao Ok(int? idGerado = null)
        => new(true, Array.Empty<string>(), idGerado);

    public static ResultadoOperacao Falha(IEnumerable<string> erros)
        => new(false, erros.ToList(), null);

    public static ResultadoOperacao Falha(params string[] erros)
        => new(false, erros, null);
}
