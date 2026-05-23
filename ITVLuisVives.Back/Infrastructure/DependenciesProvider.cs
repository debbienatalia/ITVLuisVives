using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using ITVLuisVives.Back.Cache;
using ITVLuisVives.Back.Config;
using ITVLuisVives.Back.Entity;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Repositories;
using ITVLuisVives.Back.Storage;
using ITVLuisVives.Back.Validators;
using ITVLuisVives.Back.Services;

namespace ITVLuisVives.Back.Infrastructure;

/// <summary>
/// Proveedor de dependencias centralizado para la aplicación ITV "Luis Vives".
/// Configura el contenedor de DI registrando cachés, validadores, storages, repositorios y servicios.
/// </summary>
public static class DependenciesProvider {
    
    /// <summary>
    /// Construye y configura el contenedor de inyección de dependencias.
    /// </summary>
    /// <param name="configureAdditional">Callback opcional para registrar dependencias extras (útil en Testing).</param>
    /// <returns>El proveedor de servicios <see cref="IServiceProvider"/> ya configurado.</returns>
    public static IServiceProvider BuildServiceProvider(Action<IServiceCollection>? configureAdditional = null) {
        var services = new ServiceCollection();

        CleanData();

        RegisterCaches(services);
        RegisterValidators(services);
        RegisterStorages(services);
        RegisterRepositories(services);
        RegisterServices(services);

        configureAdditional?.Invoke(services);

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Registra el motor de intercambio de archivos (Storage) según la configuración (JSON, XML o CSV).
    /// </summary>
    private static void RegisterStorages(IServiceCollection services) {
        services.AddTransient<IStorage<Cita>>(sp => {
            return AppConfig.StorageType.ToLower() switch {
                "json" => new CitaJsonStorage(),
                "xml" => new CitaXmlStorage(),
                "csv" => new CitaCsvStorage(),
                _ => new CitaJsonStorage()
            };
        });
    }

    /// <summary>
    /// Registra el repositorio de citas permitiendo alternar polimórficamente entre ADO.NET, Dapper y EF Core.
    /// </summary>
    private static void RegisterRepositories(IServiceCollection services) {
        services.AddSingleton<ICitaRepository>(sp => {
            return AppConfig.RepositoryType.ToLower() switch {
                "adonet" => CreateAdoRepository(AppConfig.DropData, AppConfig.SeedData),
                "dapper" => CreateDapperRepository(AppConfig.DropData, AppConfig.SeedData),
                "efcore" => CreateEfRepository(AppConfig.DropData, AppConfig.SeedData),
                _ => CreateEfRepository(AppConfig.DropData, AppConfig.SeedData)
            };
        });
    }

    /// <summary>
    /// Crea el repositorio basado en ADO.NET puro con SQLite.
    /// </summary>
    private static CitaAdoRepository CreateAdoRepository(bool dropData, bool seedData) {
        AsegurarCarpetaBase();
        var connection = new SqliteConnection(AppConfig.ConnectionString);
        connection.Open();
        return new CitaAdoRepository(connection, () => connection.Close(), dropData, seedData);
    }

    /// <summary>
    /// Crea el repositorio basado en Dapper con SQLite.
    /// </summary>
    private static CitaDapperRepository CreateDapperRepository(bool dropData, bool seedData) {
        AsegurarCarpetaBase();
        var connection = new SqliteConnection(AppConfig.ConnectionString);
        connection.Open();
        return new CitaDapperRepository(connection, () => connection.Close(), dropData, seedData);
    }

    /// <summary>
    /// Crea el repositorio basado en Entity Framework Core con SQLite.
    /// </summary>
    private static CitaEfRepository CreateEfRepository(bool dropData, bool seedData) {
        AsegurarCarpetaBase();
        var context = new ItvDbContext(AppConfig.ConnectionString);
        return new CitaEfRepository(context, dropData, seedData);
    }

    /// <summary>
    /// Registra los validadores de negocio encargados de verificar DNI, Matrículas y Fechas.
    /// </summary>
    private static void RegisterValidators(IServiceCollection services) {
        services.AddTransient<IValidator<Cita>, CitaValidator>();
    }

    /// <summary>
    /// Registra las cachés LRU para optimizar las lecturas por ID y por Matrícula.
    /// </summary>
    private static void RegisterCaches(IServiceCollection services) {
        services.AddSingleton<ICache<Guid, Cita>>(sp => new AppCache<Guid, Cita>(AppConfig.CacheSize));
        services.AddSingleton<ICache<string, Cita>>(sp => new AppCache<string, Cita>(AppConfig.CacheSize));
    }

    /// <summary>
    /// Registra la lógica de negocio de citas con sus dependencias básicas esenciales.
    /// </summary>
    private static void RegisterServices(IServiceCollection services) {
        services.AddScoped<ICitaService, CitaService>(sp => new CitaService(
            sp.GetRequiredService<ICitaRepository>(),
            sp.GetRequiredService<IValidator<Cita>>()
        ));
    }

    /// <summary>
    /// Garantiza la existencia del directorio base para la base de datos.
    /// </summary>
    private static void AsegurarCarpetaBase() {
        if (!Directory.Exists(AppConfig.RepositoryDirectory)) {
            Directory.CreateDirectory(AppConfig.RepositoryDirectory);
        }
    }

    /// <summary>
    /// Limpia los directorios temporales si se solicita y ejecuta la rotación obligatoria de logs (> 5 días).
    /// </summary>
    private static void CleanData() {
        try {
            if (Directory.Exists("logs")) {
                var limiteTemporal = DateTime.Now.AddDays(-5);
                foreach (var logFile in Directory.GetFiles("logs", "*.log")) {
                    if (File.GetCreationTime(logFile) < limiteTemporal) File.Delete(logFile);
                }
            }
        } catch {  }

        if (AppConfig.DropData || AppConfig.SeedData) {
            CleanDirectory(AppConfig.ReportsDirectory);
        }
    }

    /// <summary>
    /// Vacía por completo un directorio eliminando archivos y subcarpetas para volverlo a crear limpio.
    /// </summary>
    private static void CleanDirectory(string path) {
        try {
            if (Directory.Exists(path)) {
                foreach (var file in Directory.GetFiles(path)) try { File.Delete(file); } catch { }
                foreach (var dir in Directory.GetDirectories(path)) try { Directory.Delete(dir, true); } catch { }
            }
            Directory.CreateDirectory(path);
        }
        catch (Exception ex) {
            Console.WriteLine($"Warning: No se pudo limpiar el directorio {path}: {ex.Message}");
        }
    }
}