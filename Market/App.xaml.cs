using System.Globalization;
using System.Windows;
using Market.Domain.Repositories;
using Market.Infrastructure.Data;
using Market.Infrastructure.Data.Repositories;
using Market.UI;
using Market.UI.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Market;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Cultura fixa pt-BR: garante "R$ 9,90" na exibição e vírgula decimal na entrada,
        // independentemente das configurações regionais da máquina.
        var ptBr = new CultureInfo("pt-BR");
        CultureInfo.DefaultThreadCurrentCulture = ptBr;
        CultureInfo.DefaultThreadCurrentUICulture = ptBr;
        Thread.CurrentThread.CurrentCulture = ptBr;
        Thread.CurrentThread.CurrentUICulture = ptBr;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        Services = ConfigureServices(configuration);
        var logger = Services.GetRequiredService<ILogger<App>>();

        ConfigurarTratamentoGlobalDeExcecoes(logger);

        try
        {
            Services.GetRequiredService<DatabaseInitializer>().Initialize();
            Services.GetRequiredService<DataSeeder>().Seed();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Falha ao inicializar o banco de dados.");
            MessageBox.Show(
                $"Falha ao inicializar o banco de dados:\n\n{ex.Message}",
                "Mercadinho", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
            return;
        }

        // Modo utilitário: cria/valida o banco e semeia, sem abrir a UI.
        if (e.Args.Contains("--initdb"))
        {
            Shutdown(0);
            return;
        }

        // Modo diagnóstico: roda o self-test de CRUD, grava o relatório em arquivo e sai.
        if (e.Args.Contains("--test-crud"))
        {
            var report = Services.GetRequiredService<CrudSelfTest>().RunAsync().GetAwaiter().GetResult();
            var reportPath = System.IO.Path.Combine(AppContext.BaseDirectory, "crud-selftest.log");
            System.IO.File.WriteAllText(reportPath, report);
            logger.LogInformation("Relatório do self-test gravado em {Path}", reportPath);
            Shutdown(report.Contains("PASSOU") ? 0 : 1);
            return;
        }

        Services.GetRequiredService<MainWindow>().Show();
    }

    /// <summary>
    /// Captura exceções não tratadas: na thread de UI, loga e avisa sem derrubar o app;
    /// nas demais threads, ao menos loga antes de encerrar.
    /// </summary>
    private void ConfigurarTratamentoGlobalDeExcecoes(ILogger<App> logger)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            logger.LogError(args.Exception, "Exceção não tratada na interface.");
            MessageBox.Show(
                "Ocorreu um erro inesperado. A operação foi cancelada, mas o sistema continua aberto.",
                "Mercadinho", MessageBoxButton.OK, MessageBoxImage.Warning);
            args.Handled = true; // mantém o app vivo
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            logger.LogCritical(args.ExceptionObject as Exception, "Exceção fatal não tratada.");
    }

    private static ServiceProvider ConfigureServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(logging =>
        {
            logging.AddConfiguration(configuration.GetSection("Logging"));
            logging.AddDebug();
        });

        // Factory de DbContext: cada operação obtém um contexto novo e de vida curta.
        services.AddDbContextFactory<AppDbContext>((sp, options) =>
            options.UseSqlite(SqliteConnectionString.Resolve(
                sp.GetRequiredService<IConfiguration>())));

        // Repositórios são stateless (só guardam o factory) — singleton é seguro.
        services.AddSingleton(typeof(IRepository<>), typeof(Repository<>));
        services.AddSingleton<IMercadoriaRepository, MercadoriaRepository>();
        services.AddSingleton<IClienteRepository, ClienteRepository>();

        // Serviços de aplicação.
        services.AddTransient<Application.Services.MercadoriaService>();
        services.AddTransient<Application.Services.ClienteService>();
        services.AddTransient<Application.Services.VendaService>();
        services.AddTransient<Application.Services.PdvService>();
        services.AddTransient<Application.Services.HistoricoService>();
        services.AddTransient<Application.Services.RelatorioService>();
        services.AddTransient<Application.Services.FiadoService>();

        services.AddSingleton<MigrationRunner>();
        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton<DataSeeder>();
        services.AddSingleton<BackupService>();
        services.AddTransient<CrudSelfTest>();

        // UI: janela principal e views (transient — nova instância a cada navegação).
        services.AddTransient<MainWindow>();
        services.AddTransient<HomeView>();
        services.AddTransient<CadastroMercadoriaView>();
        services.AddTransient<ManterMercadoriasView>();
        services.AddTransient<EditarMercadoriaWindow>();
        services.AddTransient<ClientesView>();
        services.AddTransient<CadastrarClienteWindow>();
        // PDV singleton: preserva o carrinho ao navegar entre telas.
        services.AddSingleton<PdvView>();
        services.AddTransient<HistoricoVendasView>();
        services.AddTransient<RelatorioLucroView>();

        return services.BuildServiceProvider();
    }
}
