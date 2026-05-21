using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace ITVLuisVives.Back.Config;

/// <summary>
/// Clase estática encargada de la lectura centralizada del fichero appsettings.json.
/// </summary>
public static class AppConfig
{
    private static readonly IConfiguration Configuration;

    static AppConfig()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    // ==========================================
    // Bloque: Storage
    // ==========================================
    public static string StorageType => Configuration.GetValue<string>("Storage:Type") ?? "Json";

    // ==========================================
    // Bloque: Repository
    // ==========================================
    public static string RepositoryType => Configuration.GetValue<string>("Repository:Type") ?? "EFCore";
    public static string RepositoryDirectory => Configuration.GetValue<string>("Repository:Directory") ?? "data";
    public static string ConnectionString => Configuration.GetValue<string>("Repository:ConnectionString") ?? "Data Source=data/itv_luisvives.db";
    public static bool DropData => Configuration.GetValue<bool>("Repository:DropData", false);
    public static bool SeedData => Configuration.GetValue<bool>("Repository:SeedData", true);
    public static bool UseLogicalDelete => Configuration.GetValue<bool>("Repository:UseLogicalDelete", true);

    // ==========================================
    // Bloque: Cache
    // ==========================================
    public static int CacheSize => Configuration.GetValue<int>("Cache:Size", 5);

    // ==========================================
    // Bloque: Backup y Reports
    // ==========================================
    public static string BackupDirectory => Configuration.GetValue<string>("Backup:Directory") ?? "backup";
    public static string BackupFormat => Configuration.GetValue<string>("Backup:Format") ?? "Json";
    public static string ReportsDirectory => Configuration.GetValue<string>("Reports:Directory") ?? "reports";

    // ==========================================
    // Bloque: Itv
    // ==========================================
    public static int MaxCitasPorDniYDia => Configuration.GetValue<int>("Itv:MaxCitasPorDniYDia", 3);
    public static int PlazoMaximoDiasCita => Configuration.GetValue<int>("Itv:PlazoMaximoDiasCita", 30);

    // ==========================================
    // Bloque: Development
    // ==========================================
    public static bool IsDevelopmentEnabled => Configuration.GetValue<bool>("Development:Enabled", false);
}