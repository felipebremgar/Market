using System.Windows;
using Market.Infrastructure.Data;
using Market.UI;
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

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        Services = ConfigureServices(configuration);
        var logger = Services.GetRequiredService<ILogger<App>>();

        try
        {
            Services.GetRequiredService<DatabaseInitializer>().Initialize();
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

        // Modo utilitário (Market.exe --initdb): cria/valida o banco e sai sem abrir a UI.
        if (e.Args.Contains("--initdb"))
        {
            Shutdown(0);
            return;
        }

        Services.GetRequiredService<MainWindow>().Show();
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

        services.AddSingleton<DatabaseInitializer>();
        services.AddTransient<MainWindow>();

        return services.BuildServiceProvider();
    }
}
