using System;
using FluentAssertions;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Models;

namespace ITVLuisVives.Test.Models;

[TestFixture]
public class VehiculoTests {

    [TestFixture]
    public class CasosPositivos {

        [Test]
        public void AsignacionPropiedades_Y_ValoresPorDefecto_DeberianSerCorrectos() {
            // Arrange
            var fechaMatriculacion = new DateTime(2022, 3, 15);

            // Act
            var vehiculo = new Vehiculo {
                Id = 42,
                Matricula = "1234ABC",
                Marca = "Toyota",
                Modelo = "Yaris",
                Motor = TipoMotor.Híbrido,
                FechaMatriculacion = fechaMatriculacion
            };

            // Assert
            vehiculo.Id.Should().Be(42);
            vehiculo.Matricula.Should().Be("1234ABC");
            vehiculo.Marca.Should().Be("Toyota");
            vehiculo.Modelo.Should().Be("Yaris");
            vehiculo.Motor.Should().Be(TipoMotor.Híbrido);
            vehiculo.FechaMatriculacion.Should().Be(fechaMatriculacion);
            vehiculo.IsDeleted.Should().BeFalse();
            vehiculo.DeletedAt.Should().BeNull();
            vehiculo.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(2));
        }

        [Test]
        public void AntiguedadAnios_DeberiaCalcularLaDiferenciaCorrectamente() {
            // Arrange
            var añoMatriculacion = 2020;
            var vehiculo = new Vehiculo { 
                FechaMatriculacion = new DateTime(añoMatriculacion, 1, 1) 
            };

            // Act
            var resultado = vehiculo.AntiguedadAnios;

            // Assert
            var añosEsperados = DateTime.Today.Year - añoMatriculacion;
            resultado.Should().Be(añosEsperados);
        }

        [Test]
        public void Equals_MismaMatriculaDiferentesDatos_DeberiaSerVerdadero() {
            // Arrange
            var v1 = new Vehiculo { Id = 1, Matricula = "1234ABC", Marca = "Toyota", Modelo = "Yaris" };
            var v2 = new Vehiculo { Id = 2, Matricula = "1234abc", Marca = "Ford", Modelo = "Focus" }; // Distinto case e id

            // Act
            var resultado = v1.Equals(v2);

            // Assert
            resultado.Should().BeTrue();
            (v1 == v2).Should().BeTrue(); // Operador de igualdad sobreestructurado del record
        }

        [Test]
        public void GetHashCode_MismaMatriculaDistintoCase_DeberiaRetornarMismoHashCode() {
            // Arrange
            var v1 = new Vehiculo { Matricula = "1234ABC" };
            var v2 = new Vehiculo { Matricula = "1234abc" };

            // Act
            var hash1 = v1.GetHashCode();
            var hash2 = v2.GetHashCode();

            // Assert
            hash1.Should().Be(hash2);
        }

        [Test]
        public void Record_MutacionConWith_DeberiaMantenerEstructuraInmutable() {
            // Arrange
            var v1 = new Vehiculo { Matricula = "1234ABC", Marca = "Audi", Modelo = "A3" };

            // Act
            var vModificado = v1 with { Modelo = "A4" };

            // Assert
            vModificado.Matricula.Should().Be("1234ABC");
            vModificado.Marca.Should().Be("Audi");
            vModificado.Modelo.Should().Be("A4");
        }
    }

    [TestFixture]
    public class CasosNegativos {

        [Test]
        public void Equals_Nulo_DeberiaRetornarFalse() {
            // Arrange
            var vehiculo = new Vehiculo { Matricula = "1234ABC" };

            // Act
            var resultado = vehiculo.Equals(null);

            // Assert
            resultado.Should().BeFalse();
        }

        [Test]
        public void Equals_DiferenteMatricula_DeberiaRetornarFalse() {
            // Arrange
            var v1 = new Vehiculo { Matricula = "1234ABC" };
            var v2 = new Vehiculo { Matricula = "5678XYZ" };

            // Act
            var resultado = v1.Equals(v2);

            // Assert
            resultado.Should().BeFalse();
            (v1 == v2).Should().BeFalse();
        }
    }
}