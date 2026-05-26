using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITVLuisVives.Back.Services;

namespace ITVLuisVives.Front.ViewModels;

public partial class InformesViewModel : ObservableObject
{
    private readonly ICitaService _citaService;
    private readonly IReportService _reportService;

    [ObservableProperty]
    private string _matriculaBuscar = "";

    [ObservableProperty]
    private bool _incluirAnuladas;

    [ObservableProperty]
    private bool _soloHoy;

    [ObservableProperty]
    private string _statusMessage = "Sistema listo. Seleccione una opción de exportación.";

    public InformesViewModel(ICitaService citaService, IReportService reportService)
    {
        _citaService = citaService;
        _reportService = reportService;
    }

    // ====================================================================
    // INFORME DE UNA CITA INDIVIDUAL
    // ====================================================================

    [RelayCommand]
    private void GenerarFichaPdf()
    {
        if (string.IsNullOrWhiteSpace(MatriculaBuscar))
        {
            StatusMessage = "Por favor, escriba una matrícula.";
            return;
        }

        var citas = _citaService.ObtenerFiltradas(
            dni: null, matricula: MatriculaBuscar, estado: null,
            fechaDesde: null, fechaHasta: null,
            pagina: 1, tamanoPagina: 1,
            incluirEliminados: true
        )?.ToList();

        if (citas == null || !citas.Any())
        {
            StatusMessage = $"No existe ninguna cita con la matrícula {MatriculaBuscar}.";
            return;
        }

        var resHtml = _reportService.GenerarInformeCitasHtml(citas);
        if (resHtml.IsSuccess)
        {
            var resPdf = _reportService.GuardarInformePdf(resHtml.Value, $"Ficha_Cita_{MatriculaBuscar}.pdf");
            StatusMessage = resPdf.IsSuccess ? $"Ficha PDF guardada ({MatriculaBuscar})." : "Error al guardar el PDF.";
        }
        else
        {
            StatusMessage = "Error al estructurar el informe.";
        }
    }

    [RelayCommand]
    private void GenerarFichaHtml()
    {
        if (string.IsNullOrWhiteSpace(MatriculaBuscar))
        {
            StatusMessage = "Por favor, escriba una matrícula.";
            return;
        }

        var citas = _citaService.ObtenerFiltradas(
            dni: null, matricula: MatriculaBuscar, estado: null,
            fechaDesde: null, fechaHasta: null,
            pagina: 1, tamanoPagina: 1,
            incluirEliminados: true
        )?.ToList();

        if (citas == null || !citas.Any())
        {
            StatusMessage = $"No se encontró la matrícula {MatriculaBuscar}.";
            return;
        }

        var res = _reportService.GenerarInformeCitasHtml(citas);
        if (res.IsSuccess)
        {
            var guardado = _reportService.GuardarInforme(res.Value, $"Ficha_Cita_{MatriculaBuscar}.html");
            StatusMessage = guardado.IsSuccess ? $"Ficha HTML guardada ({MatriculaBuscar})." : "Error al guardar el HTML.";
        }
        else
        {
            StatusMessage = "Error en el servicio de informes.";
        }
    }

    // ====================================================================
    // INFORME GLOBAL (TODAS LAS CITAS FILTRADAS)
    // ====================================================================

    [RelayCommand]
    private void ReporteGlobalPdf()
    {
        DateTime? fechaFiltro = SoloHoy ? DateTime.Today.AddHours(10) : null;

        var citas = _citaService.ObtenerFiltradas(
            dni: null, matricula: null, estado: null,
            fechaDesde: fechaFiltro, fechaHasta: fechaFiltro,
            pagina: 1, tamanoPagina: int.MaxValue,
            incluirEliminados: IncluirAnuladas
        )?.ToList();

        if (citas == null || !citas.Any())
        {
            StatusMessage = "No se encontraron citas con los filtros seleccionados.";
            return;
        }

        var resHtml = _reportService.GenerarInformeCitasHtml(citas);
        if (resHtml.IsSuccess)
        {
            var resPdf = _reportService.GuardarInformePdf(resHtml.Value, "Listado_General_Citas.pdf");
            StatusMessage = resPdf.IsSuccess ? $"PDF global generado con {citas.Count} registros." : "Error al guardar PDF global.";
        }
        else
        {
            StatusMessage = "Error de renderizado base.";
        }
    }

    [RelayCommand]
    private void ReporteGlobalHtml()
    {
        DateTime? fechaFiltro = SoloHoy ? DateTime.Today.AddHours(10) : null;

        var citas = _citaService.ObtenerFiltradas(
            dni: null, matricula: null, estado: null,
            fechaDesde: fechaFiltro, fechaHasta: fechaFiltro,
            pagina: 1, tamanoPagina: int.MaxValue,
            incluirEliminados: IncluirAnuladas
        )?.ToList();

        if (citas == null || !citas.Any())
        {
            StatusMessage = "No hay registros disponibles para el informe HTML global.";
            return;
        }

        var res = _reportService.GenerarInformeCitasHtml(citas);
        if (res.IsSuccess)
        {
            var guardado = _reportService.GuardarInforme(res.Value, "Listado_General_Citas.html");
            StatusMessage = guardado.IsSuccess ? $"HTML global generado con {citas.Count} registros." : "Error al guardar HTML global.";
        }
        else
        {
            StatusMessage = "Error en la estructura del listado.";
        }
    }
}