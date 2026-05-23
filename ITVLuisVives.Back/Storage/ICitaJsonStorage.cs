using ITVLuisVives.Back.Models;

namespace ITVLuisVives.Back.Storage;

/// <summary>
/// Contrato exclusivo para la persistencia de citas en formato JSON.
/// </summary>
public interface ICitaJsonStorage : IStorage<Cita> { }