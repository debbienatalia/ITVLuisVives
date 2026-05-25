using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CSharpFunctionalExtensions;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Models;
using SelectPdf;
using Serilog;

namespace ITVLuisVives.Back.Services;

/// <summary>
/// Servicio para la generación de informes y fichas técnicas de la ITV.
/// Genera fichas individuales e informes globales en formatos HTML y PDF con diseño.
/// </summary>
public class ReportService : IReportService {
    private const string DateFormat = "dd/MM/yyyy";
    private readonly ILogger _logger = Log.ForContext<ReportService>();
    private readonly string _reportDirectory;

    public ReportService(string reportDirectory) {
        _reportDirectory = reportDirectory;
        _logger.Debug("Inicializando la clase ReportService con directorio {Directory}", _reportDirectory);
    }

    /// <inheritdoc />
    public Result<string, DomainError> GenerarFichaCitaHtml(Cita cita) {
        _logger.Information("Generando ficha técnica HTML para la cita del vehículo {Matricula}", cita.VehiculoMatricula);

        try {
            var fechaEmision = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            var fechaCita = FormatDate(cita.FechaInspeccion);
            var horaCita = cita.FechaInspeccion.ToString("HH:mm");
            
            var observaciones = string.IsNullOrWhiteSpace(cita.Observaciones) 
                ? "Sin observaciones ni defectos registrados." 
                : cita.Observaciones;

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>Ficha ITV</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 30px; color: #333; line-height: 1.5; }}
        .header {{ border-bottom: 2px solid #2c3e50; padding-bottom: 10px; margin-bottom: 25px; }}
        .header h1 {{ margin: 0; color: #2c3e50; font-size: 24px; }}
        .header p {{ margin: 5px 0 0; color: #7f8c8d; font-size: 13px; }}
        .section-title {{ font-size: 15px; font-weight: bold; color: #2c3e50; margin: 20px 0 10px 0; text-transform: uppercase; border-bottom: 1px solid #ddd; padding-bottom: 4px; }}
        .data-table {{ width: 100%; border-collapse: collapse; margin-bottom: 20px; }}
        .data-table td {{ padding: 10px; border: 1px solid #e0e0e0; font-size: 14px; width: 50%; }}
        .label {{ font-weight: bold; color: #555; display: inline-block; width: 120px; }}
        .obs-box {{ background: #f9f9f9; padding: 15px; border: 1px solid #e0e0e0; font-size: 13px; border-left: 4px solid #2c3e50; }}
        .footer {{ margin-top: 50px; text-align: center; font-size: 11px; color: #95a5a6; border-top: 1px solid #eee; padding-top: 10px; }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>Ficha de Inspección Técnica</h1>
        <p>Documento de Control Interno — Emitido el {fechaEmision}</p>
    </div>
    
    <div class=""section-title"">Datos de la Cita</div>
    <table class=""data-table"">
        <tr>
            <td><span class=""label"">Referencia Cita:</span> {cita.Id}</td>
            <td><span class=""label"">Estado:</span> <strong>{cita.Estado.ToString().ToUpper()}</strong></td>
        </tr>
        <tr>
            <td><span class=""label"">Fecha:</span> {fechaCita}</td>
            <td><span class=""label"">Hora:</span> {horaCita} h</td>
        </tr>
    </table>

    <div class=""section-title"">Datos del Vehículo y Conductor</div>
    <table class=""data-table"">
        <tr>
            <td><span class=""label"">Matrícula:</span> <strong>{cita.VehiculoMatricula}</strong></td>
            <td><span class=""label"">DNI Conductor:</span> {cita.Dni}</td>
        </tr>
    </table>

    <div class=""section-title"">Observaciones y Defectos</div>
    <div class=""obs-box"">{observaciones}</div>

    <div class=""footer"">
        Estación de Inspección Técnica ITV Luis Vives
    </div>
</body>
</html>";

            _logger.Information("Ficha HTML generada correctamente para la cita {Id}", cita.Id);
            return Result.Success<string, DomainError>(html);
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error al generar la ficha HTML de la cita {Id}", cita.Id);
            return Result.Failure<string, DomainError>(
                new DomainError("Report.GenerationError", $"Fallo al generar HTML: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Result<string, DomainError> GenerarInformeCitasHtml(IEnumerable<Cita> citas) {
        _logger.Information("Generando informe HTML consolidado de citas");

        try {
            var lista = citas.ToList();
            var fecha = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            
            var total = lista.Count;
            var completadas = lista.Count(c => c.Estado.ToString().Equals("Favorable", StringComparison.OrdinalIgnoreCase));
            var rechazadas = lista.Count(c => c.Estado.ToString().Equals("Desfavorable", StringComparison.OrdinalIgnoreCase));
            var pendientes = total - (completadas + rechazadas);

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>Informe de Citas</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 25px; color: #333; }}
        h1 {{ color: #2c3e50; font-size: 22px; margin-bottom: 5px; }}
        .meta-info {{ font-size: 12px; color: #7f8c8d; margin-bottom: 20px; }}
        .summary-bar {{ background: #f2f4f4; border: 1px solid #d5dbdb; padding: 12px; margin-bottom: 20px; font-size: 13px; }}
        .summary-bar span {{ margin-right: 20px; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 10px; }}
        th {{ background-color: #34495e; color: white; padding: 8px 10px; font-size: 13px; text-align: left; }}
        td {{ padding: 8px 10px; border-bottom: 1px solid #e0e0e0; font-size: 13px; }}
        tr:nth-child(even) {{ background-color: #fcfcfc; }}
    </style>
</head>
<body>
    <h1>Informe General de Actividad ITV</h1>
    <div class=""meta-info"">Fecha de generación: {fecha}</div>
    
    <div class=""summary-bar"">
        <strong>Resumen Estadístico:</strong>
        <span>Total Registros: {total}</span>
        <span>Favorables: {completadas}</span>
        <span>Desfavorables: {rechazadas}</span>
        <span>Pendientes: {pendientes}</span>
    </div>

    <table>
        <thead>
            <tr>
                <th>ID Cita</th>
                <th>Matrícula</th>
                <th>DNI Conductor</th>
                <th>Fecha / Hora</th>
                <th>Estado</th>
            </tr>
        </thead>
        <tbody>";

            foreach (var c in lista.OrderBy(x => x.FechaInspeccion)) {
                var fechaCita = FormatDate(c.FechaInspeccion) + " " + c.FechaInspeccion.ToString("HH:mm");

                html += $@"
            <tr>
                <td>{c.Id}</td>
                <td><strong>{c.VehiculoMatricula}</strong></td>
                <td>{c.Dni}</td>
                <td>{fechaCita}</td>
                <td>{c.Estado.ToString().ToUpper()}</td>
            </tr>";
            }

            html += @"
        </tbody>
    </table>
</body>
</html>";

            _logger.Information("Informe HTML consolidado generado correctamente");
            return Result.Success<string, DomainError>(html);
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error al generar informe HTML consolidado de citas");
            return Result.Failure<string, DomainError>(
                new DomainError("Report.GenerationError", $"Fallo al generar informe: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Result<bool, DomainError> GuardarInforme(string html, string fileName) {
        var directory = _reportDirectory;
        _logger.Information("Guardando informe HTML en directorio {Directory}", directory);

        try {
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            var filePath = Path.Combine(directory, fileName);
            File.WriteAllText(filePath, html, Encoding.UTF8);

            _logger.Information("Informe HTML guardado correctamente en {FilePath}", filePath);
            return Result.Success<bool, DomainError>(true);
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error al escribir el informe HTML en disco");
            return Result.Failure<bool, DomainError>(
                new DomainError("Report.SaveError", $"No se pudo escribir el archivo: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Result<bool, DomainError> GuardarInformePdf(string html, string fileName) {
        var directory = _reportDirectory;
        _logger.Information("Guardando informe PDF en directorio {Directory}", directory);

        try {
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            var filePath = Path.Combine(directory, fileName);

            var converter = new HtmlToPdf();
            converter.Options.PdfPageSize = PdfPageSize.A4;
            converter.Options.MarginTop = 15;
            converter.Options.MarginBottom = 15;
            converter.Options.MarginLeft = 15;
            converter.Options.MarginRight = 15;

            var doc = converter.ConvertHtmlString(html);
            doc.Save(filePath);
            doc.Close();

            _logger.Information("Informe PDF guardado correctamente en {FilePath}", filePath);
            return Result.Success<bool, DomainError>(true);
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error al procesar o guardar el informe PDF");
            return Result.Failure<bool, DomainError>(
                new DomainError("Report.SaveError", $"No se pudo generar el PDF: {ex.Message}"));
        }
    }

    private string FormatDate(DateTime date) {
        return date.ToString(DateFormat);
    }
}