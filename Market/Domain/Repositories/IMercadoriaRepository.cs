using Market.Domain;

namespace Market.Domain.Repositories;

/// <summary>Repositório de mercadorias com consultas específicas do domínio.</summary>
public interface IMercadoriaRepository : IRepository<Mercadoria>
{
    /// <summary>
    /// Indica se já existe mercadoria com o código de barras informado.
    /// <paramref name="ignorarId"/> exclui um registro da checagem (útil na edição — Dia 4).
    /// </summary>
    Task<bool> CodigoBarrasExisteAsync(string codigoBarras, int? ignorarId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Busca uma mercadoria ativa pelo código de barras (usado no PDV — Dia 6).</summary>
    Task<Mercadoria?> ObterPorCodigoBarrasAsync(string codigoBarras,
        CancellationToken cancellationToken = default);
}
