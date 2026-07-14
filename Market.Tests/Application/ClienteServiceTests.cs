using Market.Application.Services;
using Market.Infrastructure.Data.Repositories;
using Market.Tests.Infra;
using Microsoft.Extensions.Logging.Abstractions;

namespace Market.Tests.Application;

public class ClienteServiceTests
{
    private const string CpfValido = "52998224725";

    private static ClienteService CriarServico(BancoDeTeste banco)
        => new(new ClienteRepository(banco), NullLogger<ClienteService>.Instance);

    [Fact]
    public async Task Cadastro_valido_persiste_com_cpf_normalizado()
    {
        using var banco = new BancoDeTeste();
        var servico = CriarServico(banco);

        var resultado = await servico.CadastrarAsync("529.982.247-25", "Maria Silva");

        Assert.True(resultado.Sucesso);
        using var context = banco.CreateDbContext();
        var cliente = context.Clientes.Single();
        Assert.Equal(CpfValido, cliente.Cpf); // normalizado (só dígitos)
        Assert.Equal("Maria Silva", cliente.Nome);
    }

    [Fact]
    public async Task Cpf_com_digito_invalido_e_rejeitado()
    {
        using var banco = new BancoDeTeste();
        var servico = CriarServico(banco);

        var resultado = await servico.CadastrarAsync("52998224724", "João");

        Assert.False(resultado.Sucesso);
        Assert.Contains(resultado.Erros, e => e.Contains("CPF"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Nome_vazio_e_rejeitado(string nome)
    {
        using var banco = new BancoDeTeste();
        var servico = CriarServico(banco);

        var resultado = await servico.CadastrarAsync(CpfValido, nome);

        Assert.False(resultado.Sucesso);
        Assert.Contains(resultado.Erros, e => e.Contains("nome", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Cpf_duplicado_e_rejeitado()
    {
        using var banco = new BancoDeTeste();
        var servico = CriarServico(banco);
        await servico.CadastrarAsync(CpfValido, "Primeiro");

        var resultado = await servico.CadastrarAsync("529.982.247-25", "Segundo");

        Assert.False(resultado.Sucesso);
        Assert.Contains(resultado.Erros, e => e.Contains("CPF"));
    }

    [Fact]
    public async Task Busca_por_nome_parcial_encontra()
    {
        using var banco = new BancoDeTeste();
        var servico = CriarServico(banco);
        await servico.CadastrarAsync(CpfValido, "Ana Paula");

        var encontrados = await servico.BuscarAsync(null, "ana");

        Assert.Single(encontrados);
        Assert.Equal("Ana Paula", encontrados[0].Nome);
    }

    [Fact]
    public async Task Busca_por_cpf_exato_encontra()
    {
        using var banco = new BancoDeTeste();
        var servico = CriarServico(banco);
        await servico.CadastrarAsync(CpfValido, "Carlos");

        var encontrados = await servico.BuscarAsync("529.982.247-25", null);

        Assert.Single(encontrados);
        Assert.Equal(CpfValido, encontrados[0].Cpf);
    }
}
