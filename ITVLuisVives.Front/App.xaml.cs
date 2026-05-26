using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Debugging;
using ITVLuisVives.Back.Config;
using ITVLuisVives.Back.Infrastructure;
using ITVLuisVives.Front.ViewModels;
using ITVLuisVives.Front.Views;
using Microsoft.Extensions.Configuration;

namespace ITVLuisVives.Front;

/// <summary>
///     Punto de entrada de la aplicación WPF.
///     Configura el entorno, los logs y la inyección de dependencias de forma lineal.
/// </summary>
public partial class App : Application 
{
    /// <summary>
    ///     Proveedor de servicios global de la aplicación.
    /// </summary>
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    /// <summary>
    ///     Método de arranque síncrono.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e) 
    {
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        ConfigureSerilog();

        Log.Information("Aplicación ITV Luis Vives iniciada.");

        ServiceProvider = DependenciesProvider.BuildServiceProvider(services => 
        {
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<CitasViewModel>();
            services.AddTransient<AcercaDeViewModel>();
            services.AddTransient<AcercaDeWindow>();
            services.AddTransient<InformesViewModel>();
            services.AddTransient<ImportExportViewModel>();
        });

        Log.Information("Contenedor de dependencias listo.");

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;

        var viewModel = ServiceProvider.GetRequiredService<MainWindowViewModel>();
        mainWindow.DataContext = viewModel;

        Log.Information("Mostrando ventana principal.");
        mainWindow.Show();

        base.OnStartup(e);
    }

    /// <summary>
    ///     Configura Serilog leyendo el archivo appsettings.json local.
    /// </summary>
    private void ConfigureSerilog() 
    {
        SelfLog.Enable(msg => Debug.WriteLine($"SERILOG DIAG: {msg}"));

        var localConfig = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(localConfig)
            .Enrich.FromLogContext()
            .CreateLogger();

        Log.Information("Serilog cargado correctamente.");
    }

    /// <summary>
    ///     Cierre limpio de la aplicación descorchando los buffers de log.
    /// </summary>
    protected override void OnExit(ExitEventArgs e) 
    {
        Log.Information("Aplicación cerrándose.");
        Log.CloseAndFlush();

        if (ServiceProvider is IDisposable disposable) 
        {
            disposable.Dispose();
        }

        base.OnExit(e);
    }
}