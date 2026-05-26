using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Storage;

namespace ITVLuisVives.Back.Factories;

public interface IStorageFactory
{
    IStorage<Cita> Crear(string path);
}