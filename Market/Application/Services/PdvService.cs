using Market.Domain;
using Market.Domain.Repositories;

namespace Market.Application.Services;

/// <summary>
/// Consultas de produto para o PDV: localizar por código de barras (bipe) e buscar por nome.
/// Ambas retornam apenas mercadorias ativas.
/// </summary>
public class PdvService
{
    private readonly IMercadoriaRepository _repositorio;

    public PdvService(IMercadoriaRepository repositorio) => _repositorio = repositorio;

    public Task<Mercadoria?> LocalizarPorCodigoAsync(string codigoBarras, CancellationToken cancellationToken = default)
        => _repositorio.ObterPorCodigoBarrasAsync(codigoBarras.Trim(), cancellationToken);

    public Task<IReadOnlyList<Mercadoria>> BuscarPorNomeAsync(string nome, CancellationToken cancellationToken = default)
        => _repositorio.ListarAsync(new FiltroMercadoria { Nome = nome?.Trim() }, cancellationToken);
}
