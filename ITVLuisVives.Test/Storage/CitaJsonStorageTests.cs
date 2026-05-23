using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Storage;

namespace ITVLuisVives.Back.Test.Storage;

[TestFixture]
public class CitaJsonStorageTests {
    [SetUp]
    public void SetUp() {
        _storage = new CitaJsonStorage();
        _tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
    }

    [TearDown]
    public void TearDown() {
        if (File.Exists(_tempPath)) File.Delete(_tempPath);
    }

    private CitaJsonStorage _storage = null!;
    private string _tempPath = null!;

    [TestFixture]
    public class CasosPositivos {
        [SetUp]
        public void SetUp() {
            _storage = new CitaJsonStorage();
            _tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        }

        [TearDown]
        public void TearDown() {
            if (File.Exists(_tempPath)) File.Delete(_tempPath);
        }

        private CitaJsonStorage _storage = null!;
        private string _tempPath = null!;

        [Test]
        public void Salvar_ConDatosValidos_DeberiaGuardarCorrectamente() {
            // Arrange
            var citas = new List<Cita> {
                new Cita {
                    Id = Guid.NewGuid(), Dni = "12345678A", VehiculoMatricula = "1234ABC",
                    FechaInspeccion = DateTime.Today.AddDays(10), Estado = EstadoCita.Pendiente,
                    Observaciones = "Sin observaciones", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
                    IsDeleted = false, DeletedAt = null
                }
            };

            // Act
            var resultado = _storage.Salvar(citas, _tempPath);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            File.Exists(_tempPath).Should().BeTrue();
        }

        [Test]
        public void Cargar_ConArchivoExistente_DeberiaRetornarDatos() {
            // Arrange
            var citas = new List<Cita> {
                new Cita {
                    Id = Guid.NewGuid(), Dni = "12345678A", VehiculoMatricula = "1234ABC",
                    FechaInspeccion = DateTime.Today.AddDays(5), Estado = EstadoCita.Pendiente,
                    Observaciones = "Revisión ordinaria", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
                    IsDeleted = false, DeletedAt = null
                }
            };
            _storage.Salvar(citas, _tempPath);

            // Act
            var resultado = _storage.Cargar(_tempPath);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().HaveCount(1);
            resultado.Value.First().Dni.Should().Be("12345678A");
            resultado.Value.First().Should().BeOfType<Cita>();
        }

        [Test]
        public void Salvar_ListaVacia_DeberiaCrearArchivoVacio() {
            // Arrange
            var citas = new List<Cita>();

            // Act
            var resultado = _storage.Salvar(citas, _tempPath);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            File.Exists(_tempPath).Should().BeTrue();
        }
    }

    [TestFixture]
    public class CasosNegativos {
        [SetUp]
        public void SetUp() {
            _storage = new CitaJsonStorage();
            _tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        }

        [TearDown]
        public void TearDown() {
            if (File.Exists(_tempPath)) File.Delete(_tempPath);
        }

        private CitaJsonStorage _storage = null!;
        private string _tempPath = null!;

        [Test]
        public void Cargar_CuandoArchivoNoExiste_DeberiaRetornarError() {
            // Arrange & Act
            var resultado = _storage.Cargar("ruta/inexistente.json");

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("FILE_NOT_FOUND");
            resultado.Error.Message.Should().Contain("ruta/inexistente.json");
        }

        [Test]
        public void Salvar_EnRutaInvalida_DeberiaRetornarError() {
            // Arrange
            var citas = new List<Cita>();

            // Act
            var resultado = _storage.Salvar(citas, Path.Combine("C:", "ruta", "invalida", "archivo.json"));

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("STORAGE_WRITE_ERROR");
            resultado.Error.Message.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void Cargar_ConArchivoCorrupto_DeberiaRetornarError() {
            // Arrange
            File.WriteAllText(_tempPath, "{ json_invalido: ... ");

            // Act
            var resultado = _storage.Cargar(_tempPath);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("STORAGE_READ_ERROR");
        }
    }

    [TestFixture]
    public class CasosMixtos {
        [SetUp]
        public void SetUp() {
            _storage = new CitaJsonStorage();
            _tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        }

        [TearDown]
        public void TearDown() {
            if (File.Exists(_tempPath)) File.Delete(_tempPath);
        }

        private CitaJsonStorage _storage = null!;
        private string _tempPath = null!;

        [Test]
        public void SalvarYLeer_RoundTrip_DeberiaMantenerDatos() {
            // Arrange
            var original = new List<Cita> {
                new Cita {
                    Id = Guid.NewGuid(), Dni = "11111111H", VehiculoMatricula = "1111AAA",
                    FechaInspeccion = DateTime.Today.AddDays(1), Estado = EstadoCita.Pendiente,
                    Observaciones = "Frenos", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false, DeletedAt = null
                },
                new Cita {
                    Id = Guid.NewGuid(), Dni = "22222222J", VehiculoMatricula = "2222BBB",
                    FechaInspeccion = DateTime.Today.AddDays(2), Estado = EstadoCita.Apta,
                    Observaciones = "Favorable", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false, DeletedAt = null
                }
            };

            // Act
            var saveResult = _storage.Salvar(original, _tempPath);
            var loadResult = _storage.Cargar(_tempPath);

            // Assert
            saveResult.IsSuccess.Should().BeTrue();
            loadResult.IsSuccess.Should().BeTrue();
            loadResult.Value.Should().HaveCount(2);

            var primeraCita = loadResult.Value.First();
            primeraCita.Should().NotBeNull();
            primeraCita.Dni.Should().Be("11111111H");
            primeraCita.VehiculoMatricula.Should().Be("1111AAA");
            primeraCita.Estado.Should().Be(EstadoCita.Pendiente);

            var segundaCita = loadResult.Value.Last();
            segundaCita.Should().NotBeNull();
            segundaCita.Dni.Should().Be("22222222J");
            segundaCita.Estado.Should().Be(EstadoCita.Apta);
        }

        [Test]
        public void Salvar_ConCitaEliminada_DeberiaMantenerEstadoEliminado() {
            // Arrange
            var citas = new List<Cita> {
                new Cita {
                    Id = Guid.NewGuid(), Dni = "12345678H", VehiculoMatricula = "9999ZZZ",
                    FechaInspeccion = DateTime.Today, Estado = EstadoCita.NoApta,
                    IsDeleted = true, DeletedAt = DateTime.UtcNow
                }
            };

            // Act
            _storage.Salvar(citas, _tempPath);
            var resultado = _storage.Cargar(_tempPath);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.First().IsDeleted.Should().BeTrue();
            resultado.Value.First().DeletedAt.Should().NotBeNull();
        }

        [Test]
        public void Inicializacion_CuandoFaltaDirectorioData_DeberiaCrearlo() {
            // Arrange
            if (Directory.Exists("data")) Directory.Delete("data", true);

            // Act
            var nuevoStorage = new CitaJsonStorage();

            // Assert
            Directory.Exists("data").Should().BeTrue();
        }

        [Test]
        public void Cargar_CuandoArchivoContieneNullLiteral_DeberiaRetornarListaVacia() {
            // Arrange
            File.WriteAllText(_tempPath, "null");

            // Act
            var resultado = _storage.Cargar(_tempPath);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().BeEmpty();
        }
    }
}