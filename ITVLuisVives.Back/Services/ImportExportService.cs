using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Storage;
using Serilog;

namespace ITVLuisVives.Back.Services;

/// <summary>
/// Servicio para importar y exportar datos de citas de la ITV.
/// Permite exportar a múltiples formatos e importar desde ellos a través del almacenamiento configurado.
/// </summary>
public class ImportExportService(
    IStorage<Cita> storage
) : IImportExportService {
    private readonly ILogger _logger = Log.ForContext<ImportExportService>();

    /// <inheritdoc />
    public Result<int, DomainError> ExportarDatos(IEnumerable<Cita> citas, string path) {
        _logger.Information("Exportando datos a {Path}", path);
        var lista = citas.ToList();
        return storage.Salvar(lista, path)
            .Map(_ => lista.Count);
    }

    /// <inheritdoc />
    public Result<IEnumerable<Cita>, DomainError> ImportarDatos(string path) {
        _logger.Information("Importando datos desde {Path}", path);
        return storage.Cargar(path);
    }

    /// <inheritdoc />
    public Result<int, DomainError> ExportarDatosSistema(IEnumerable<Cita> citas) {
        return ExportarDatos(citas, string.Empty);
    }

    /// <inheritdoc />
    public Result<IEnumerable<Cita>, DomainError> ImportarDatosSistema(string path) {
        return ImportarDatos(path);
    }
}