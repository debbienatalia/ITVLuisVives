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
using NUnit.Framework;

namespace ITVLuisVives.Test.Mappers;

[TestFixture]
public class VehiculoMapperTests
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    [TestFixture]
    public class CasosPositivos : VehiculoMapperTests
    {
        [SetUp]
        public void SetUp()
        {
            _vehiculo = new Vehiculo
            {
                Id = 1,
                Matricula = "1234BBB",
                Marca = "Seat",
                Modelo = "Ibiza",
                Motor = TipoMotor.Diesel,
                FechaMatriculacion = new DateTime(2020, 5, 10),
                CreatedAt = new DateTime(2026, 1, 1, 10, 0, 0),
                UpdatedAt = new DateTime(2026, 1, 2, 12, 0, 0),
                IsDeleted = false,
                DeletedAt = null
            };

            _dtoVehiculo = new VehiculoDto(
                1, "1234BBB", "Seat", "Ibiza", "Diesel", 
                "05/10/2020", 6, 
                "2026-01-01T10:00:00", "2026-01-02T12:00:00", 
                false, null);

            _entityVehiculo = new VehiculoEntity
            {
                Id = 1,
                Matricula = "1234BBB",
                Marca = "Seat",
                Modelo = "Ibiza",
                Motor = "Diesel",
                FechaMatriculacion = new DateTime(2020, 5, 10),
                CreatedAt = new DateTime(2026, 1, 1, 10, 0, 0),
                UpdatedAt = new DateTime(2026, 1, 2, 12, 0, 0),
                IsDeleted = false,
                DeletedAt = null
            };
        }

        private Vehiculo _vehiculo = null!;
        private VehiculoDto _dtoVehiculo = null!;
        private VehiculoEntity _entityVehiculo = null!;

        [Test]
        public void ToModel_EntityValida_DeberiaConvertirCorrectamente()
        {
            // Act
            var resultado = _entityVehiculo.ToModel();

            // Assert
            resultado.Should().NotBeNull();
            resultado!.Id.Should().Be(_entityVehiculo.Id);
            resultado.Matricula.Should().Be(_entityVehiculo.Matricula);
            resultado.Marca.Should().Be(_entityVehiculo.Marca);
            resultado.Modelo.Should().Be(_entityVehiculo.Modelo);
            resultado.Motor.Should().Be(TipoMotor.Diesel);
            resultado.FechaMatriculacion.Should().Be(_entityVehiculo.FechaMatriculacion);
            resultado.IsDeleted.Should().BeFalse();
        }

        [Test]
        public void ToEntity_ModelValido_DeberiaConvertirCorrectamente()
        {
            // Act
            var resultado = _vehiculo.ToEntity();

            // Assert
            resultado.Should().NotBeNull();
            resultado.Id.Should().Be(_vehiculo.Id);
            resultado.Matricula.Should().Be(_vehiculo.Matricula);
            resultado.Marca.Should().Be(_vehiculo.Marca);
            resultado.Modelo.Should().Be(_vehiculo.Modelo);
            resultado.Motor.Should().Be("Diesel");
        }

        [Test]
        public void ToDto_ModelValido_DeberiaConvertirCorrectamente()
        {
            // Act
            var resultado = _vehiculo.ToDto();

            // Assert
            resultado.Should().NotBeNull();
            resultado.Id.Should().Be(_vehiculo.Id);
            resultado.Matricula.Should().Be(_vehiculo.Matricula);
            resultado.Motor.Should().Be("Diesel");
            resultado.FechaMatriculacion.Should().Be(_vehiculo.FechaMatriculacion.ToString("d", Invariant));
            resultado.AntiguedadAnios.Should().Be(DateTime.Today.Year - _vehiculo.FechaMatriculacion.Year);
        }

        [Test]
        public void ToModel_DtoValido_DeberiaConvertirCorrectamente()
        {
            // Arrange
            var dtoIso = new VehiculoDto(
                1, "1234BBB", "Seat", "Ibiza", "Diesel", 
                "2020-05-10", 6, 
                "2026-01-01T10:00:00", "2026-01-02T12:00:00", 
                false, null);

            // Act
            var resultado = dtoIso.ToModel();

            // Assert
            resultado.Should().NotBeNull();
            resultado!.Id.Should().Be(dtoIso.Id);
            resultado.Matricula.Should().Be(dtoIso.Matricula);
            resultado.Marca.Should().Be(dtoIso.Marca);
            resultado.Modelo.Should().Be(dtoIso.Modelo);
            resultado.Motor.Should().Be(TipoMotor.Diesel);
        }

        [Test]
        public void ToModel_ListaEntities_DeberiaConvertirTodosFiltrandoNulls()
        {
            // Arrange
            var entities = new List<VehiculoEntity> { _entityVehiculo, null! };

            // Act
            var resultado = entities.ToModel().ToList();

            // Assert
            resultado.Should().HaveCount(1);
            resultado.First().Id.Should().Be(_entityVehiculo.Id);
        }

        [Test]
        public void ToDto_ConIsDeleted_DeberiaMantenerEstadoYFechas()
        {
            // Arrange
            var vehiculoEliminado = _vehiculo with { 
                IsDeleted = true, 
                DeletedAt = new DateTime(2026, 1, 3, 14, 0, 0) 
            };

            // Act
            var resultado = vehiculoEliminado.ToDto();

            // Assert
            resultado.IsDeleted.Should().BeTrue();
            resultado.DeletedAt.Should().Be("2026-01-03T14:00:00");
        }

        [Test]
        public void Model_GetHashCode_DeberiaSerBasadoEnMatricula()
        {
            // Arrange
            var vehiculo1 = new Vehiculo { Matricula = "1234BBB" };
            var vehiculo2 = new Vehiculo { Matricula = "1234bbb" };

            // Act & Assert
            vehiculo1.GetHashCode().Should().Be(vehiculo2.GetHashCode());
        }
    }

    [TestFixture]
    public class CasosNegativos : VehiculoMapperTests
    {
        [Test]
        public void ToModel_EntityNulo_DeberiaRetornarNull()
        {
            // Act
            var resultado = ((VehiculoEntity?)null).ToModel();

            // Assert
            resultado.Should().BeNull();
        }

        [Test]
        public void ToModel_EntityConMotorInvalido_DeberiaUsarGasolinaPorDefecto()
        {
            // Arrange
            var entityInvalida = new VehiculoEntity { Motor = "Kerosene" };

            // Act
            var resultado = entityInvalida.ToModel();

            // Assert
            resultado.Should().NotBeNull();
            resultado!.Motor.Should().Be(TipoMotor.Gasolina);
        }

        [Test]
        public void ToEntity_ModelConValoresNulos_DeberiaUsarStringsVacios()
        {
            // Arrange
            var modelNulo = new Vehiculo
            {
                Matricula = null!,
                Marca = null!,
                Modelo = null!,
                Motor = TipoMotor.Eléctrico
            };

            // Act
            var resultado = modelNulo.ToEntity();

            // Assert
            resultado.Matricula.Should().Be(string.Empty);
            resultado.Marca.Should().Be(string.Empty);
            resultado.Modelo.Should().Be(string.Empty);
        }

        [Test]
        public void ToDto_ModelConValoresNulos_DeberiaUsarStringsVaciosYDeletedAtNull()
        {
            // Arrange
            var modelNulo = new Vehiculo
            {
                Matricula = null!,
                Marca = null!,
                Modelo = null!,
                Motor = TipoMotor.Gasolina,
                DeletedAt = null
            };

            // Act
            var resultado = modelNulo.ToDto();

            // Assert
            resultado.Matricula.Should().Be(string.Empty);
            resultado.Marca.Should().Be(string.Empty);
            resultado.Modelo.Should().Be(string.Empty);
            resultado.DeletedAt.Should().BeNull();
        }

        [Test]
        public void ToModel_DtoConMotorInvalidoYDeletedAtVacio_DeberiaAsignarValoresPorDefecto()
        {
            // Arrange
            var dtoInvalido = new VehiculoDto(
                1, "1234BBB", "Seat", "Ibiza", "BIO_DIESEL_INVALIDO", 
                "2020-05-10", 6, 
                "2026-01-01T10:00:00", "2026-01-02T12:00:00", 
                false, "");

            // Act
            var resultado = dtoInvalido.ToModel();

            // Assert
            resultado!.Motor.Should().Be(TipoMotor.Gasolina);
            resultado.DeletedAt.Should().BeNull();
        }

        [Test]
        public void ToModel_DtoConDeletedAtNull_DeberiaAsignarDeletedAtNull()
        {
            // Arrange
            var dtoInvalido = new VehiculoDto(
                1, "1234BBB", "Seat", "Ibiza", "Gasolina", 
                "2020-05-10", 6, 
                "2026-01-01T10:00:00", "2026-01-02T12:00:00", 
                false, null);

            // Act
            var resultado = dtoInvalido.ToModel();

            // Assert
            resultado!.DeletedAt.Should().BeNull();
        }
    }
}