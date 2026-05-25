using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CSharpFunctionalExtensions;
using FluentAssertions;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Services;
using ITVLuisVives.Back.Storage;
using Moq;

namespace ITVLuisVives.Back.Test.Services;

[TestFixture]
public class ImportExportServiceTests {
    [SetUp]
    public void SetUp() {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ImportExportTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        _storageMock = new Mock<IStorage<Cita>>();
        _service = new ImportExportService(_storageMock.Object);
    }

    [TearDown]
    public void TearDown() {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string _tempDir = null!;
    private ImportExportService _service = null!;
    private Mock<IStorage<Cita>> _storageMock = null!;

    [TestFixture]
    public class CasosPositivos : ImportExportServiceTests {
        [Test]
        public void ExportarDatos_ConCitas_DeberiaRetornarContador() {
            // Arrange
            var citas = new List<Cita> {
                new Cita { VehiculoMatricula = "1234ABC", Dni = "11111111A" },
                new Cita { VehiculoMatricula = "5678DEF", Dni = "22222222B" },
                new Cita { VehiculoMatricula = "9012GHI", Dni = "33333333C" }
            };

            var path = Path.Combine(_tempDir, "export.json");
            _storageMock.Setup(s => s.Salvar(It.IsAny<IEnumerable<Cita>>(), It.IsAny<string>()))
                .Returns(Result.Success<bool, DomainError>(true));

            // Act
            var resultado = _service.ExportarDatos(citas, path);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().Be(3);
        }

        [Test]
        public void ImportarDatos_ConArchivo_DeberiaRetornarCitas() {
            // Arrange
            var citas = new List<Cita> {
                new Cita { VehiculoMatricula = "1234ABC", Dni = "11111111A" }
            };
            var path = Path.Combine(_tempDir, "import.json");

            _storageMock.Setup(s => s.Cargar(path))
                .Returns(Result.Success<IEnumerable<Cita>, DomainError>(citas));

            // Act
            var resultado = _service.ImportarDatos(path);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().HaveCount(1);
            resultado.Value.First().VehiculoMatricula.Should().Be("1234ABC");
        }

        [Test]
        public void ExportarDatosSistema_DeberiaLLamarExportarDatosConRutaVacia() {
            // Arrange
            var citas = new List<Cita> { new Cita { VehiculoMatricula = "1234ABC" } };
            _storageMock.Setup(s => s.Salvar(It.IsAny<IEnumerable<Cita>>(), It.IsAny<string>()))
                .Returns(Result.Success<bool, DomainError>(true));

            // Act
            var resultado = _service.ExportarDatosSistema(citas);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            _storageMock.Verify(s => s.Salvar(It.IsAny<IEnumerable<Cita>>(), string.Empty), Times.Once);
        }

        [Test]
        public void ImportarDatosSistema_ConRuta_DeberiaLLamarImportarDatos() {
            // Arrange
            var path = Path.Combine(_tempDir, "test.json");
            var citas = new List<Cita> { new Cita { VehiculoMatricula = "1234ABC" } };

            _storageMock.Setup(s => s.Cargar(path))
                .Returns(Result.Success<IEnumerable<Cita>, DomainError>(citas));

            // Act
            var resultado = _service.ImportarDatosSistema(path);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            _storageMock.Verify(s => s.Cargar(path), Times.Once);
        }

        [Test]
        public void ExportarDatos_ConListaVacia_DeberiaRetornarCero() {
            // Arrange
            var citas = new List<Cita>();
            var path = Path.Combine(_tempDir, "empty.json");

            _storageMock.Setup(s => s.Salvar(It.IsAny<IEnumerable<Cita>>(), It.IsAny<string>()))
                .Returns(Result.Success<bool, DomainError>(true));

            // Act
            var resultado = _service.ExportarDatos(citas, path);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().Be(0);
        }
    }

    [TestFixture]
    public class CasosNegativos : ImportExportServiceTests {
        [Test]
        public void ImportarDatos_ConError_DeberiaRetornarError() {
            // Arrange
            var path = Path.Combine(_tempDir, "no-existe.json");
            var error = new TestError("File not found", "Storage.LoadError");

            _storageMock.Setup(s => s.Cargar(path))
                .Returns(Result.Failure<IEnumerable<Cita>, DomainError>(error));

            // Act
            var resultado = _service.ImportarDatos(path);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("Storage.LoadError");
        }

        [Test]
        public void ExportarDatos_ConError_DeberiaRetornarError() {
            // Arrange
            var citas = new List<Cita> { new Cita { VehiculoMatricula = "1234ABC" } };
            var path = Path.Combine(_tempDir, "error.json");
            var error = new TestError("Write error", "Storage.SaveError");

            _storageMock.Setup(s => s.Salvar(It.IsAny<IEnumerable<Cita>>(), It.IsAny<string>()))
                .Returns(Result.Failure<bool, DomainError>(error));

            // Act
            var resultado = _service.ExportarDatos(citas, path);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("Storage.SaveError");
        }
        
        private record TestError(string Message, string Code) : DomainError(Message, Code);    }
}