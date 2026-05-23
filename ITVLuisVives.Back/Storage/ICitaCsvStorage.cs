using ITVLuisVives.Back.Models;

namespace ITVLuisVives.Back.Storage;

/// <summary>
/// Contrato exclusivo para la persistencia de citas en formato CSV.
/// </summary>
public interface ICitaCsvStorage : IStorage<Cita> { }