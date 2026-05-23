using System.Collections.Generic;
using CSharpFunctionalExtensions;
using ITVLuisVives.Back.Errors;

namespace ITVLuisVives.Back.Storage;

/// <summary>
/// Interfaz genérica para la persistencia e importación de colecciones de objetos.
/// </summary>
public interface IStorage<T> {
    /// <summary>
    /// Guarda una colección de objetos en la ruta especificada.
    /// </summary>
    /// <returns>True si tiene éxito; de lo contrario, un DomainError de escritura o formato.</returns>
    Result<bool, DomainError> Salvar(IEnumerable<T> items, string path);

    /// <summary>
    /// Carga una colección de objetos desde la ruta especificada.
    /// </summary>
    /// <returns>La colección de objetos o un DomainError de lectura o archivo no encontrado.</returns>
    Result<IEnumerable<T>, DomainError> Cargar(string path);
}