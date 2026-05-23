using ITVLuisVives.Back.Models;

namespace ITVLuisVives.Back.Storage;

/// <summary>
/// Contrato exclusivo para la persistencia de citas en formato XML.
/// </summary>
public interface ICitaXmlStorage : IStorage<Cita> { }