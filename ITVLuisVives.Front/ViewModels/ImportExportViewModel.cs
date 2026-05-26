using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITVLuisVives.Back.Services;
using ITVLuisVives.Back.Models;

namespace ITVLuisVives.Front.ViewModels;

public partial class ImportExportViewModel : ObservableObject
{
    private readonly ICitaService _citaService;
    private readonly IImportExportService _importExportService;

    [ObservableProperty]
    private bool _sustituirDatos;

    [ObservableProperty]
    private string _statusMessage = "Listo para realizar operaciones de persistencia externa.";

    public ImportExportViewModel(ICitaService citaService, IImportExportService importExportService)
    {
        _citaService = citaService;
        _importExportService = importExportService;
    }

    // ====================================================================
    // COMANDOS DE EXPORTACIÓN
    // ====================================================================

    [RelayCommand]
    private void ExportarJson() => EjecutarExportacion("JSON (*.json)|*.json", "json");

    [RelayCommand]
    private void ExportarXml() => EjecutarExportacion("XML (*.xml)|*.xml", "xml");

    [RelayCommand]
    private void ExportarCsv() => EjecutarExportacion("CSV (*.csv)|*.csv", "csv");

    private void EjecutarExportacion(string filtro, string extension)
    {
        var todasLasCitas = _citaService.ObtenerFiltradas(
            dni: null, matricula: null, estado: null, fechaDesde: null, fechaHasta: null,
            pagina: 1, tamanoPagina: int.MaxValue, incluirEliminados: true
        ).ToList();

        var sfd = new SaveFileDialog 
        { 
            Filter = filtro, 
            DefaultExt = extension,
            FileName = $"Backup_ITV_{DateTime.Now:yyyyMMdd}.{extension}"
        };
        
        if (sfd.ShowDialog() == true)
        {
            var resultado = _importExportService.ExportarDatos(todasLasCitas, sfd.FileName);

            if (resultado.IsSuccess)
            {
                StatusMessage = $"Éxito: Se exportaron {resultado.Value} citas en formato .{extension.ToUpper()} correctamente.";
            }
            else
            {
                StatusMessage = $"Error de dominio: {resultado.Error.Message}";
            }
        }
    }

    // ====================================================================
    // COMANDOS DE IMPORTACIÓN
    // ====================================================================

    [RelayCommand]
    private void ImportarJson() => EjecutarImportacion("JSON (*.json)|*.json");

    [RelayCommand]
    private void ImportarXml() => EjecutarImportacion("XML (*.xml)|*.xml");

    [RelayCommand]
    private void ImportarCsv() => EjecutarImportacion("CSV (*.csv)|*.csv");

    private void EjecutarImportacion(string filtro)
    {
        var ofd = new OpenFileDialog { Filter = filtro };

        if (ofd.ShowDialog() == true)
        {
            var resultado = _importExportService.ImportarDatos(ofd.FileName);

            if (resultado.IsSuccess)
            {
                var citasImportadas = resultado.Value.ToList();
                
                if (SustituirDatos)
                {
                    StatusMessage = "Purgando base de datos activa antes de la importación...";
                    
                    var actuales = _citaService.ObtenerFiltradas(
                        dni: null, matricula: null, estado: null, fechaDesde: null, fechaHasta: null,
                        pagina: 1, tamanoPagina: int.MaxValue, incluirEliminados: true
                    ).ToList();

                    foreach (var citaActiva in actuales)
                    {
                        _citaService.Cancelar(citaActiva.Id); 
                    }
                }

                int exitos = 0;
                int fallos = 0;

                foreach (var cita in citasImportadas)
                {
                    var resAgendar = _citaService.Agendar(cita);
                    if (resAgendar.IsSuccess)
                        exitos++;
                    else
                        fallos++;
                }

                StatusMessage = $"Importación finalizada. Éxitos: {exitos} | Rechazados por reglas de negocio: {fallos}.";
            }
            else
            {
                StatusMessage = $"Error al importar el archivo: {resultado.Error.Message}";
            }
        }
    }
}