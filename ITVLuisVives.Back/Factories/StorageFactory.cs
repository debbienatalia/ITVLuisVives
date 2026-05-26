using System.IO;
using ITVLuisVives.Back.Config;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Storage;

namespace ITVLuisVives.Back.Factories;

public class StorageFactory : IStorageFactory
{
    public IStorage<Cita> Crear(string path)
    {
        var extension = Path.GetExtension(path)?
            .ToLower()
            .TrimStart('.') ?? string.Empty;

        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = AppConfig.StorageType.ToLower();
        }

        return extension switch
        {
            "json" => new CitaJsonStorage(),
            "xml" => new CitaXmlStorage(),
            "csv" => new CitaCsvStorage(),
            _ => new CitaJsonStorage()
        };
    }
}