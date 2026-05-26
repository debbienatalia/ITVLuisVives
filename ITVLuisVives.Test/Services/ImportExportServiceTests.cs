using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CSharpFunctionalExtensions;
using FluentAssertions;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Factories;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Services;
using ITVLuisVives.Back.Storage;
using Moq;

namespace ITVLuisVives.Back.Test.Services;

[TestFixture]
public class ImportExportServiceTests
{
    [TestFixture]
    public class CasosPositivos
    {
        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(
                Path.GetTempPath(),
                $"ImportExportTest_{Guid.NewGuid()}");

            Directory.CreateDirectory(_tempDir);

            _storageMock = new Mock<IStorage<Cita>>();
            _factoryMock = new Mock<IStorageFactory>();

            _factoryMock
                .Setup(f => f.Crear(It.IsAny<string>()))
                .Returns(_storageMock.Object);

            _service = new ImportExportService(_factoryMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        private string _tempDir = null!;
        private Mock<IStorage<Cita>> _storageMock = null!;
        private Mock<IStorageFactory> _factoryMock = null!;
        private ImportExportService _service = null!;

        [Test]
        public void ExportarDatos_ConCitas_DeberiaRetornarContador()
        {
            // Arrange
            var citas = new List<Cita>
            {
                new()
                {
                    VehiculoMatricula = "1234ABC",
                    Dni = "11111111A"
                },
                new()
                {
                    VehiculoMatricula = "5678DEF",
                    Dni = "22222222B"
                }
            };

            var path = Path.Combine(_tempDir, "export.json");

            _storageMock
                .Setup(s => s.Salvar(It.IsAny<IEnumerable<Cita>>(), path))
                .Returns(Result.Success<bool, DomainError>(true));

            // Act
            var resultado = _service.ExportarDatos(citas, path);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().Be(2);

            _factoryMock.Verify(
                f => f.Crear(path),
                Times.Once);

            _storageMock.Verify(
                s => s.Salvar(It.IsAny<IEnumerable<Cita>>(), path),
                Times.Once);
        }

        [Test]
        public void ImportarDatos_ConArchivo_DeberiaRetornarCitas()
        {
            // Arrange
            var citas = new List<Cita>
            {
                new()
                {
                    VehiculoMatricula = "1234ABC"
                }
            };

            var path = Path.Combine(_tempDir, "import.json");

            _storageMock
                .Setup(s => s.Cargar(path))
                .Returns(Result.Success<IEnumerable<Cita>, DomainError>(citas));

            // Act
            var resultado = _service.ImportarDatos(path);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().HaveCount(1);

            _factoryMock.Verify(
                f => f.Crear(path),
                Times.Once);

            _storageMock.Verify(
                s => s.Cargar(path),
                Times.Once);
        }

        [Test]
        public void ExportarDatosSistema_DeberiaLLamarExportarDatosConRutaVacia()
        {
            // Arrange
            var citas = new List<Cita>
            {
                new()
                {
                    VehiculoMatricula = "1234ABC"
                }
            };

            _storageMock
                .Setup(s => s.Salvar(It.IsAny<IEnumerable<Cita>>(), string.Empty))
                .Returns(Result.Success<bool, DomainError>(true));

            // Act
            var resultado = _service.ExportarDatosSistema(citas);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().Be(1);

            _factoryMock.Verify(
                f => f.Crear(string.Empty),
                Times.Once);

            _storageMock.Verify(
                s => s.Salvar(It.IsAny<IEnumerable<Cita>>(), string.Empty),
                Times.Once);
        }

        [Test]
        public void ImportarDatosSistema_ConRuta_DeberiaLLamarImportarDatos()
        {
            // Arrange
            var path = Path.Combine(_tempDir, "sistema.json");

            var citas = new List<Cita>
            {
                new()
                {
                    VehiculoMatricula = "9999XYZ"
                }
            };

            _storageMock
                .Setup(s => s.Cargar(path))
                .Returns(Result.Success<IEnumerable<Cita>, DomainError>(citas));

            // Act
            var resultado = _service.ImportarDatosSistema(path);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().HaveCount(1);

            _factoryMock.Verify(
                f => f.Crear(path),
                Times.Once);

            _storageMock.Verify(
                s => s.Cargar(path),
                Times.Once);
        }

        [Test]
        public void ExportarDatos_ConEnumerableYield_DeberiaCubrirForeachCompleto()
        {
            // Arrange
            IEnumerable<Cita> Generar()
            {
                yield return new Cita
                {
                    VehiculoMatricula = "1111AAA"
                };

                yield return new Cita
                {
                    VehiculoMatricula = "2222BBB"
                };
            }

            var path = Path.Combine(_tempDir, "yield.json");

            _storageMock
                .Setup(s => s.Salvar(It.IsAny<IEnumerable<Cita>>(), path))
                .Returns(Result.Success<bool, DomainError>(true));

            // Act
            var resultado = _service.ExportarDatos(Generar(), path);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().Be(2);
        }

        [Test]
        public void ExportarDatos_ConEnumerableVacio_DeberiaCubrirRamaForeachSinIteraciones()
        {
            // Arrange
            IEnumerable<Cita> citas = Enumerable.Empty<Cita>();

            var path = Path.Combine(_tempDir, "empty.json");

            _storageMock
                .Setup(s => s.Salvar(It.IsAny<IEnumerable<Cita>>(), path))
                .Returns(Result.Success<bool, DomainError>(true));

            // Act
            var resultado = _service.ExportarDatos(citas, path);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().Be(0);
        }

        [Test]
        public void ImportarDatos_ConListaVacia_DeberiaRetornarColeccionVacia()
        {
            // Arrange
            var path = Path.Combine(_tempDir, "empty_import.json");

            _storageMock
                .Setup(s => s.Cargar(path))
                .Returns(Result.Success<IEnumerable<Cita>, DomainError>(
                    Enumerable.Empty<Cita>()));

            // Act
            var resultado = _service.ImportarDatos(path);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().BeEmpty();
        }
    }

    [TestFixture]
    public class CasosNegativos
    {
        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(
                Path.GetTempPath(),
                $"ImportExportTest_{Guid.NewGuid()}");

            Directory.CreateDirectory(_tempDir);

            _storageMock = new Mock<IStorage<Cita>>();
            _factoryMock = new Mock<IStorageFactory>();

            _factoryMock
                .Setup(f => f.Crear(It.IsAny<string>()))
                .Returns(_storageMock.Object);

            _service = new ImportExportService(_factoryMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        private string _tempDir = null!;
        private Mock<IStorage<Cita>> _storageMock = null!;
        private Mock<IStorageFactory> _factoryMock = null!;
        private ImportExportService _service = null!;

        [Test]
        public void ImportarDatos_ConError_DeberiaRetornarError()
        {
            // Arrange
            var path = Path.Combine(_tempDir, "no-existe.json");

            var error = new TestError(
                "File not found",
                "Storage.LoadError");

            _storageMock
                .Setup(s => s.Cargar(path))
                .Returns(Result.Failure<IEnumerable<Cita>, DomainError>(error));

            // Act
            var resultado = _service.ImportarDatos(path);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("Storage.LoadError");
        }

        [Test]
        public void ExportarDatos_ConError_DeberiaRetornarError()
        {
            // Arrange
            var citas = new List<Cita>
            {
                new()
                {
                    VehiculoMatricula = "1234ABC"
                }
            };

            var path = Path.Combine(_tempDir, "error.json");

            var error = new TestError(
                "Write error",
                "Storage.SaveError");

            _storageMock
                .Setup(s => s.Salvar(It.IsAny<IEnumerable<Cita>>(), path))
                .Returns(Result.Failure<bool, DomainError>(error));

            // Act
            var resultado = _service.ExportarDatos(citas, path);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("Storage.SaveError");
        }

        [Test]
        public void ExportarDatosSistema_ConError_DeberiaPropagarFailure()
        {
            // Arrange
            var citas = new List<Cita>
            {
                new()
                {
                    VehiculoMatricula = "FAIL123"
                }
            };

            var error = new TestError(
                "system export failed",
                "SYS_EXPORT_ERROR");

            _storageMock
                .Setup(s => s.Salvar(It.IsAny<IEnumerable<Cita>>(), string.Empty))
                .Returns(Result.Failure<bool, DomainError>(error));

            // Act
            var resultado = _service.ExportarDatosSistema(citas);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("SYS_EXPORT_ERROR");
        }

        [Test]
        public void ImportarDatosSistema_ConFailure_DeberiaPropagarError()
        {
            // Arrange
            var path = Path.Combine(_tempDir, "broken.json");

            var error = new TestError(
                "system import failed",
                "SYS_IMPORT_ERROR");

            _storageMock
                .Setup(s => s.Cargar(path))
                .Returns(Result.Failure<IEnumerable<Cita>, DomainError>(error));

            // Act
            var resultado = _service.ImportarDatosSistema(path);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("SYS_IMPORT_ERROR");
        }

        private record TestError(string Message, string Code)
            : DomainError(Message, Code);
    }

    [TestFixture]
    public class CasosMixtos
    {
        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(
                Path.GetTempPath(),
                $"ImportExportTest_{Guid.NewGuid()}");

            Directory.CreateDirectory(_tempDir);

            _storageMock = new Mock<IStorage<Cita>>();
            _factoryMock = new Mock<IStorageFactory>();

            _factoryMock
                .Setup(f => f.Crear(It.IsAny<string>()))
                .Returns(_storageMock.Object);

            _service = new ImportExportService(_factoryMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        private string _tempDir = null!;
        private Mock<IStorage<Cita>> _storageMock = null!;
        private Mock<IStorageFactory> _factoryMock = null!;
        private ImportExportService _service = null!;

        [Test]
        public void ExportarDatos_ConArray_DeberiaProcesarseCorrectamente()
        {
            // Arrange
            var citas = new[]
            {
                new Cita
                {
                    VehiculoMatricula = "1111AAA"
                }
            };

            var path = Path.Combine(_tempDir, "array.json");

            _storageMock
                .Setup(s => s.Salvar(It.IsAny<IEnumerable<Cita>>(), path))
                .Returns(Result.Success<bool, DomainError>(true));

            // Act
            var resultado = _service.ExportarDatos(citas, path);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().Be(1);
        }

        [Test]
        public void ExportarDatos_ConEnumerableDiferido_DeberiaEnumerarseCorrectamente()
        {
            // Arrange
            IEnumerable<Cita> citas = Enumerable
                .Range(1, 3)
                .Select(i => new Cita
                {
                    VehiculoMatricula = $"MAT{i}"
                });

            var path = Path.Combine(_tempDir, "deferred.json");

            _storageMock
                .Setup(s => s.Salvar(It.IsAny<IEnumerable<Cita>>(), path))
                .Returns(Result.Success<bool, DomainError>(true));

            // Act
            var resultado = _service.ExportarDatos(citas, path);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().Be(3);
        }

        [Test]
        public void ExportarDatos_ConMuchosElementos_DeberiaRetornarCantidadCorrecta()
        {
            // Arrange
            var citas = Enumerable
                .Range(1, 50)
                .Select(i => new Cita
                {
                    VehiculoMatricula = $"TEST{i}"
                });

            var path = Path.Combine(_tempDir, "bulk.json");

            _storageMock
                .Setup(s => s.Salvar(It.IsAny<IEnumerable<Cita>>(), path))
                .Returns(Result.Success<bool, DomainError>(true));

            // Act
            var resultado = _service.ExportarDatos(citas, path);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().Be(50);
        }
    }
}