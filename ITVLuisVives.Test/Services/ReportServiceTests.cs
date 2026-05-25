using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FluentAssertions;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Services;

namespace ITVLuisVives.Test.Services;

[TestFixture]
public class ReportServiceTests {
    private ReportService _service = null!;
    private CultureInfo _originalCulture = null!;
    private string _tempDirPath = null!;

    [SetUp]
    public void SetUp() {
        _tempDirPath = Path.Combine(Path.GetTempPath(), $"ITVReportTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirPath);

        _service = new ReportService(_tempDirPath);

        _originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("es-ES");
        CultureInfo.CurrentUICulture = new CultureInfo("es-ES");
    }

    [TearDown]
    public void TearDown() {
        CultureInfo.CurrentCulture = _originalCulture;
        CultureInfo.CurrentUICulture = _originalCulture;

        if (Directory.Exists(_tempDirPath)) {
            try {
                Directory.Delete(_tempDirPath, true);
            }
            catch {
                
            }
        }
    }

    [TestFixture]
    public class CasosPositivos : ReportServiceTests {
        [Test]
        public void GenerarInformeCitasHtml_ConListaVacia_DeberiaCalcularEstadisticasEnCero() {
            // Arrange
            var citasVacias = new List<Cita>();

            // Act
            var resultado = _service.GenerarInformeCitasHtml(citasVacias);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().Contain("Total Registros: 0");
            resultado.Value.Should().Contain("Favorables: 0");
        }
        
        [Test]
        public void GenerarFichaCitaHtml_ConObservaciones_DeberiaGenerarHtmlCorrecto() {
            // Arrange
            var citaId = Guid.NewGuid();
            var cita = new Cita {
                Id = citaId,
                VehiculoMatricula = "1234XYZ",
                Dni = "11122233A",
                FechaInspeccion = new DateTime(2026, 05, 25, 10, 30, 0),
                Estado = EstadoCita.Apta,
                Observaciones = "Todo en perfecto estado."
            };

            // Act
            var resultado = _service.GenerarFichaCitaHtml(cita);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().Contain("1234XYZ");
            resultado.Value.Should().Contain("Todo en perfecto estado.");
        }

        [Test]
        public void GenerarFichaCitaHtml_SinObservaciones_DeberiaAsignarTextoPorDefecto() {
            // Arrange
            var cita = new Cita {
                Id = Guid.NewGuid(),
                VehiculoMatricula = "5678ABC",
                Estado = EstadoCita.NoApta,
                Observaciones = "   "
            };

            // Act
            var resultado = _service.GenerarFichaCitaHtml(cita);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().Contain("Sin observaciones ni defectos registrados.");
        }

        [TestCase(EstadoCita.Pendiente)]
        [TestCase(EstadoCita.EnProgreso)]
        [TestCase(EstadoCita.Apta)]
        [TestCase(EstadoCita.NoApta)]
        [TestCase(EstadoCita.Cancelada)]
        public void GenerarFichaCitaHtml_ParaCadaEstadoDelEnum_DeberiaProcesarTodasLasRamasHtml(EstadoCita estado) {
            // Arrange
            var cita = new Cita {
                Id = Guid.NewGuid(),
                VehiculoMatricula = "TEST-ENUM",
                Estado = estado,
                Observaciones = "Muestra de estado"
            };

            // Act
            var resultado = _service.GenerarFichaCitaHtml(cita);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.ToUpper().Should().Contain(estado.ToString().ToUpper());
        }

        [Test]
        public void GenerarInformeCitasHtml_ConTodosLosEstadosPosibles_DeberiaCalcularEstadisticasYCombinaciones() {
            // Arrange
            var citas = new List<Cita> {
                new() { Id = Guid.NewGuid(), VehiculoMatricula = "MAT1", Estado = EstadoCita.Apta, FechaInspeccion = new DateTime(2026, 05, 25, 9, 0, 0) },
                new() { Id = Guid.NewGuid(), VehiculoMatricula = "MAT2", Estado = EstadoCita.NoApta, FechaInspeccion = new DateTime(2026, 05, 25, 11, 0, 0) },
                new() { Id = Guid.NewGuid(), VehiculoMatricula = "MAT3", Estado = EstadoCita.Pendiente, FechaInspeccion = new DateTime(2026, 05, 25, 10, 0, 0) },
                new() { Id = Guid.NewGuid(), VehiculoMatricula = "MAT4", Estado = EstadoCita.EnProgreso, FechaInspeccion = new DateTime(2026, 05, 25, 12, 0, 0) },
                new() { Id = Guid.NewGuid(), VehiculoMatricula = "MAT5", Estado = EstadoCita.Cancelada, FechaInspeccion = new DateTime(2026, 05, 25, 13, 0, 0) }
            };

            // Act
            var resultado = _service.GenerarInformeCitasHtml(citas);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            var html = resultado.Value;
            html.Should().Contain("Total Registros: 5");
            html.Should().Contain("MAT1");
            html.Should().Contain("MAT5");
        }

        [Test]
        public void GuardarInforme_CuandoElDirectorioNoExiste_DeberiaCrearloYPersistir() {
            // Arrange
            var html = "<html></html>";
            var fileName = "informe_nuevo.html";
            if (Directory.Exists(_tempDirPath)) Directory.Delete(_tempDirPath, true); // Forzar !Directory.Exists -> True

            // Act
            var resultado = _service.GuardarInforme(html, fileName);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
        }

        [Test]
        public void GuardarInforme_CuandoElDirectorioYaExiste_DeberiaEscribirDirectamente() {
            // Arrange
            var html = "<html></html>";
            var fileName = "informe_existente.html";

            // Act
            var resultado = _service.GuardarInforme(html, fileName);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            File.Exists(Path.Combine(_tempDirPath, fileName)).Should().BeTrue();
        }

        [Test]
        public void GuardarInformePdf_CuandoElDirectorioNoExiste_DeberiaCrearloYConvertir() {
            // Arrange
            var html = "<html><body>PDF</body></html>";
            var fileName = "pdf_nuevo.pdf";
            if (Directory.Exists(_tempDirPath)) Directory.Delete(_tempDirPath, true);

            // Act
            var resultado = _service.GuardarInformePdf(html, fileName);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
        }

        [Test]
        public void GuardarInformePdf_CuandoElDirectorioYaExiste_DeberiaGuardarSinCrearDirectorio() {
            // Arrange
            var html = "<html><body>PDF</body></html>";
            var fileName = "pdf_existente.pdf";

            // Act
            var resultado = _service.GuardarInformePdf(html, fileName);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            File.Exists(Path.Combine(_tempDirPath, fileName)).Should().BeTrue();
        }
    }

    [TestFixture]
    public class CasosNegativos : ReportServiceTests {
        [Test]
        public void GenerarFichaCitaHtml_CuandoCitaEsNula_DeberiaRetornarGenerationError() {
            // Act
            var resultado = _service.GenerarFichaCitaHtml(null!);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Be("Report.GenerationError");
        }

        [Test]
        public void GenerarFichaCitaHtml_CuandoObservacionesEsNulo_DeberiaManejarloComoVacio() {
            // Arrange
            var cita = new Cita {
                Id = Guid.NewGuid(),
                VehiculoMatricula = "NULL-OBS",
                Observaciones = null!
            };

            // Act
            var resultado = _service.GenerarFichaCitaHtml(cita);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().Contain("Sin observaciones ni defectos registrados.");
        }

        [Test]
        public void GenerarInformeCitasHtml_CuandoColeccionEsNula_DeberiaRetornarGenerationError() {
            // Act
            var resultado = _service.GenerarInformeCitasHtml(null!);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Be("Report.GenerationError");
        }

        [Test]
        public void GuardarInforme_ConNombreDeArchivoInvalido_DeberiaRetornarSaveError() {
            // Act
            var resultado = _service.GuardarInforme("html", "error\0file.html");

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Be("Report.SaveError");
        }

        [Test]
        public void GuardarInformePdf_ConNombreDeArchivoInvalido_DeberiaRetornarSaveError() {
            // Act
            var resultado = _service.GuardarInformePdf("html", "error\0file.pdf");

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Be("Report.SaveError");
        }

        [Test]
        public void GenerarFichaCitaHtml_CuandoFallaBloqueTry_DeberiaRetornarFalloCapturadoPorElCatch() {
            // Arrange
            var cita = new Cita { Id = Guid.NewGuid(), VehiculoMatricula = "FalloCatch", Estado = EstadoCita.Apta };
            _service.ForzarErrorInyeccion = true;

            // Act
            var resultado = _service.GenerarFichaCitaHtml(cita);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Be("Report.GenerationError");
            resultado.Error.Code.Should().Contain("Simulado para cobertura");
        }

        [Test]
        public void GenerarInformeCitasHtml_CuandoFallaBloqueTry_DeberiaRetornarFalloCapturadoPorElCatch() {
            // Arrange
            var citas = new List<Cita> { new() { Id = Guid.NewGuid(), Estado = EstadoCita.Apta } };
            _service.ForzarErrorInyeccion = true;

            // Act
            var resultado = _service.GenerarInformeCitasHtml(citas);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Be("Report.GenerationError");
            resultado.Error.Code.Should().Contain("Simulado para cobertura");
        }
    }
}