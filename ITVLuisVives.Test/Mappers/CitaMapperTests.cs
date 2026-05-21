using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FluentAssertions;
using ITVLuisVives.Back.Dto;
using ITVLuisVives.Back.Entity;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Mappers;
using ITVLuisVives.Back.Models;

namespace ITVLuisVives.Test.Mappers;

[TestFixture]
public class CitaMapperTests
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    [TestFixture]
    public class CasosPositivos : CitaMapperTests
    {
        [SetUp]
        public void SetUp()
        {
            _guidId = Guid.NewGuid();

            _cita = new Cita
            {
                Id = _guidId,
                Dni = "12345678H",
                VehiculoMatricula = "1234BBB",
                FechaInspeccion = DateTime.Today,
                Estado = EstadoCita.Apta,
                Observaciones = "Sin fallos mecánicos",
                CreatedAt = new DateTime(2026, 1, 1, 10, 0, 0),
                UpdatedAt = new DateTime(2026, 1, 2, 12, 0, 0),
                IsDeleted = false,
                DeletedAt = null
            };

            _dtoCita = new CitaDto(
                _guidId.ToString(), "12345678H", "1234BBB",
                "2026-05-21T10:00:00", "Apta", "Sin fallos mecánicos",
                true, "2026-01-01T10:00:00", "2026-01-02T12:00:00",
                false, null);

            _entityCita = new CitaEntity
            {
                Id = _guidId,
                Dni = "12345678H",
                VehiculoMatricula = "1234BBB",
                FechaInspeccion = DateTime.Today,
                Estado = "Apta",
                Observaciones = "Sin fallos mecánicos",
                CreatedAt = new DateTime(2026, 1, 1, 10, 0, 0),
                UpdatedAt = new DateTime(2026, 1, 2, 12, 0, 0),
                IsDeleted = false,
                DeletedAt = null
            };
        }

        private Guid _guidId;
        private Cita _cita = null!;
        private CitaDto _dtoCita = null!;
        private CitaEntity _entityCita = null!;

        [Test]
        public void ToModel_EntityValida_DeberiaConvertirCorrectamente()
        {
            // Act
            var resultado = _entityCita.ToModel();

            // Assert
            resultado.Should().NotBeNull();
            resultado!.Id.Should().Be(_entityCita.Id);
            resultado.Dni.Should().Be(_entityCita.Dni);
            resultado.VehiculoMatricula.Should().Be(_entityCita.VehiculoMatricula);
            resultado.FechaInspeccion.Should().Be(_entityCita.FechaInspeccion);
            resultado.Estado.Should().Be(EstadoCita.Apta);
            resultado.Observaciones.Should().Be(_entityCita.Observaciones);
            resultado.IsDeleted.Should().BeFalse();
        }

        [Test]
        public void ToEntity_ModelValido_DeberiaConvertirCorrectamente()
        {
            // Act
            var resultado = _cita.ToEntity();

            // Assert
            resultado.Should().NotBeNull();
            resultado.Id.Should().Be(_cita.Id);
            resultado.Dni.Should().Be(_cita.Dni);
            resultado.VehiculoMatricula.Should().Be(_cita.VehiculoMatricula);
            resultado.Estado.Should().Be("Apta");
            resultado.Observaciones.Should().Be(_cita.Observaciones);
        }

        [Test]
        public void ToDto_ModelValido_DeberiaConvertirCorrectamente()
        {
            // Act
            var resultado = _cita.ToDto();

            // Assert
            resultado.Should().NotBeNull();
            resultado.Id.Should().Be(_cita.Id.ToString());
            resultado.Dni.Should().Be(_cita.Dni);
            resultado.VehiculoMatricula.Should().Be(_cita.VehiculoMatricula);
            resultado.Estado.Should().Be("Apta");
            resultado.FechaInspeccion.Should().Be(_cita.FechaInspeccion.ToString("s", Invariant));
            resultado.EsHoy.Should().Be(_cita.EsHoy);
        }

        [Test]
        public void ToModel_DtoValido_DeberiaConvertirCorrectamente()
        {
            // Arrange
            var dtoIso = new CitaDto(
                _guidId.ToString(), "12345678H", "1234BBB",
                "2026-05-21T15:30:00", "EnProgreso", "Sin observaciones",
                true, "2026-01-01T10:00:00", "2026-01-02T12:00:00",
                false, null);

            // Act
            var resultado = dtoIso.ToModel();

            // Assert
            resultado.Should().NotBeNull();
            resultado.Id.Should().Be(_guidId);
            resultado.Dni.Should().Be(dtoIso.Dni);
            resultado.VehiculoMatricula.Should().Be(dtoIso.VehiculoMatricula);
            resultado.Estado.Should().Be(EstadoCita.EnProgreso);
            resultado.FechaInspeccion.Should().Be(DateTime.Parse(dtoIso.FechaInspeccion, Invariant));
        }

        [Test]
        public void ToModel_ListaEntities_DeberiaConvertirTodosFiltrandoNulls()
        {
            // Arrange
            var entities = new List<CitaEntity> { _entityCita, null! };

            // Act
            var resultado = entities.ToModel().ToList();

            // Assert
            resultado.Should().HaveCount(1);
            resultado.First().Id.Should().Be(_entityCita.Id);
        }

        [Test]
        public void ToDto_ConIsDeleted_DeberiaMantenerEstadoYFechas()
        {
            // Arrange
            var citaEliminada = _cita with
            {
                IsDeleted = true,
                DeletedAt = new DateTime(2026, 1, 3, 14, 0, 0)
            };

            // Act
            var resultado = citaEliminada.ToDto();

            // Assert
            resultado.IsDeleted.Should().BeTrue();
            resultado.DeletedAt.Should().Be("2026-01-03T14:00:00");
        }
    }

    [TestFixture]
    public class CasosNegativos : CitaMapperTests
    {
        [Test]
        public void ToModel_EntityNulo_DeberiaRetornarNull()
        {
            // Act
            var resultado = ((CitaEntity?)null).ToModel();

            // Assert
            resultado.Should().BeNull();
        }

        [Test]
        public void ToModel_EntityConEstadoInvalido_DeberiaUsarPendientePorDefecto()
        {
            // Arrange
            var entityInvalida = new CitaEntity { Estado = "CompletadaOInvalida" };

            // Act
            var resultado = entityInvalida.ToModel();

            // Assert
            resultado.Should().NotBeNull();
            resultado!.Estado.Should().Be(EstadoCita.Pendiente);
        }

        [Test]
        public void ToEntity_ModelConValoresNulos_DeberiaUsarStringsVacios()
        {
            // Arrange
            var modelNulo = new Cita
            {
                Dni = null!,
                VehiculoMatricula = null!,
                Observaciones = null!,
                Estado = EstadoCita.Cancelada
            };

            // Act
            var resultado = modelNulo.ToEntity();

            // Assert
            resultado.Dni.Should().Be(string.Empty);
            resultado.VehiculoMatricula.Should().Be(string.Empty);
            resultado.Observaciones.Should().Be(string.Empty);
        }

        [Test]
        public void ToDto_ModelConValoresNulos_DeberiaUsarStringsVaciosYDeletedAtNull()
        {
            // Arrange
            var modelNulo = new Cita
            {
                Dni = null!,
                VehiculoMatricula = null!,
                Observaciones = null!,
                Estado = EstadoCita.Pendiente,
                DeletedAt = null
            };

            // Act
            var resultado = modelNulo.ToDto();

            // Assert
            resultado.Dni.Should().Be(string.Empty);
            resultado.VehiculoMatricula.Should().Be(string.Empty);
            resultado.Observaciones.Should().Be(string.Empty);
            resultado.DeletedAt.Should().BeNull();
        }

        [Test]
        public void ToModel_DtoConIdInvalidoEstadoInvalidoYDeletedAtVacio_DeberiaAsignarValoresPorDefecto()
        {
            // Arrange
            var dtoInvalido = new CitaDto(
                "ID_GUID_TOTALMENTE_INVALIDO", "12345678H", "1234BBB",
                "2026-05-21T10:00:00", "TOTALMENTE_ERRONEO", "Observación",
                false, "2026-01-01T10:00:00", "2026-01-02T12:00:00",
                false, "");

            // Act
            var resultado = dtoInvalido.ToModel();

            // Assert
            resultado.Should().NotBeNull();
            resultado.Id.Should().NotBe(Guid.Empty); 
            resultado.Estado.Should().Be(EstadoCita.Pendiente);
            resultado.DeletedAt.Should().BeNull();
        }

        [Test]
        public void ToModel_DtoConDeletedAtNull_DeberiaAsignarDeletedAtNull()
        {
            // Arrange
            var dtoInvalido = new CitaDto(
                Guid.NewGuid().ToString(), "12345678H", "1234BBB",
                "2026-05-21T10:00:00", "Pendiente", "Observación",
                false, "2026-01-01T10:00:00", "2026-01-02T12:00:00",
                false, null);

            // Act
            var resultado = dtoInvalido.ToModel();

            // Assert
            resultado.DeletedAt.Should().BeNull();
        }
    }
}