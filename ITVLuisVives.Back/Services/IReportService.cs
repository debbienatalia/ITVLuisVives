using System.Collections.Generic;
using CSharpFunctionalExtensions;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Models;

namespace ITVLuisVives.Back.Services;

/// <summary>
/// Define el contrato para la generación de informes y fichas técnicas del sistema.
/// </summary>
public interface IReportService {
    
    /// <summary>
    /// Genera la ficha técnica en formato HTML de una cita específica.
    /// </summary>
    /// <param name="cita">La cita de la que se generará la ficha.</param>
    /// <returns>Result con el HTML generado o un error de dominio.</returns>
    Result<string, DomainError> GenerarFichaCitaHtml(Cita cita);

    /// <summary>
    /// Genera un informe resumido en HTML de un listado de citas.
    /// </summary>
    /// <param name="citas">Colección de citas a incluir en el informe.</param>
    /// <returns>Result con el HTML generado o un error de dominio.</returns>
    Result<string, DomainError> GenerarInformeCitasHtml(IEnumerable<Cita> citas);

    /// <summary>
    /// Guarda el contenido HTML de un informe en un archivo físico.
    /// </summary>
    /// <param name="html">Contenido HTML.</param>
    /// <param name="path">Ruta completa del archivo de destino.</param>
    /// <returns>Result con true si se guardó correctamente o un error de dominio.</returns>
    Result<bool, DomainError> GuardarInforme(string html, string path);

    /// <summary>
    /// Convierte el contenido HTML a formato PDF y lo guarda en un archivo.
    /// </summary>
    /// <param name="html">Contenido HTML de origen.</param>
    /// <param name="path">Ruta completa del archivo PDF de destino.</param>
    /// <returns>Result con true si se generó correctamente o un error de dominio.</returns>
    Result<bool, DomainError> GuardarInformePdf(string html, string path);
}