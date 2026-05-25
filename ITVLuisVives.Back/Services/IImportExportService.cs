using System.Collections.Generic;
using CSharpFunctionalExtensions;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Models;

namespace ITVLuisVives.Back.Services;

/// <summary>
/// Interfaz para la gestión de importación y exportación de citas.
/// </summary>
public interface IImportExportService {
    
    /// <summary>
    /// Exporta citas a un archivo en la ruta especificada.
    /// </summary>
    /// <param name="citas">Colección de citas a exportar.</param>
    /// <param name="path">Ruta del archivo de destino.</param>
    /// <returns>Result con el número de citas exportadas o un error de dominio.</returns>
    Result<int, DomainError> ExportarDatos(IEnumerable<Cita> citas, string path);

    /// <summary>
    /// Importa citas desde un archivo en la ruta especificada.
    /// </summary>
    /// <param name="path">Ruta del archivo a importar.</param>
    /// <returns>Result con la lista de citas importadas o un error de dominio.</returns>
    Result<IEnumerable<Cita>, DomainError> ImportarDatos(string path);

    /// <summary>
    /// Exporta citas usando la ruta predeterminada del sistema.
    /// </summary>
    /// <param name="citas">Colección de citas a exportar.</param>
    /// <returns>Result con el número de citas exportadas o un error de dominio.</returns>
    Result<int, DomainError> ExportarDatosSistema(IEnumerable<Cita> citas);

    /// <summary>
    /// Importa citas desde la ruta predeterminada del sistema.
    /// </summary>
    /// <param name="path">Ruta del archivo a importar dentro del sistema.</param>
    /// <returns>Result con la lista de citas importadas o un error de dominio.</returns>
    Result<IEnumerable<Cita>, DomainError> ImportarDatosSistema(string path);
}