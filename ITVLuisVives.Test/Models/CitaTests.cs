using System;
using FluentAssertions;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Models;
using NUnit.Framework;

namespace ITVLuisVives.Test.Models;

[TestFixture]
public class CitaTests {

    [TestFixture]
    public class CasosPositivos {

        [Test]
        public void AsignacionPropiedades_Y_ValoresPorDefecto_DeberianSerCorrectos() {
            // Arrange
            var fecha = DateTime.Today.AddDays(5);

            // Act
            var cita = new Cita {
                Dni = "12345678A",
                VehiculoMatricula = "1234ABC",
                FechaInspeccion = fecha,
                Estado = EstadoCita.Pendiente,
                Observaciones = "Inspección ordinaria"
            };

            // Assert
            cita.Id.Should().NotBe(Guid.Empty);
            cita.Dni.Should().Be("12345678A");
            cita.VehiculoMatricula.Should().Be("1234ABC");
            cita.FechaInspeccion.Should().Be(fecha);
            cita.Estado.Should().Be(EstadoCita.Pendiente);
            cita.Observaciones.Should().Be("Inspección ordinaria");
            cita.IsDeleted.Should().BeFalse();
            cita.DeletedAt.Should().BeNull();
            cita.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(2));
        }

        [Test]
        public void EsHoy_FechaInspeccionEsHoy_DeberiaRetornarTrue() {
            // Arrange
            var cita = new Cita { FechaInspeccion = DateTime.Today };

            // Act
            var resultado = cita.EsHoy;

            // Assert
            resultado.Should().BeTrue();
        }

        [Test]
        public void EsHoy_FechaInspeccionEsDiferente_DeberiaRetornarFalse() {
            // Arrange
            var cita = new Cita { FechaInspeccion = DateTime.Today.AddDays(1) };

            // Act
            var resultado = cita.EsHoy;

            // Assert
            resultado.Should().BeFalse();
        }

        [Test]
        public void Equals_MismoIdDiferentesDatos_DeberiaSerIgual() {
            // Arrange
            var idComun = Guid.NewGuid();
            var cita1 = new Cita { Id = idComun, Dni = "11111111A", Observaciones = "Texto A" };
            var cita2 = new Cita { Id = idComun, Dni = "22222222B", Observaciones = "Texto B" };

            // Act
            var resultado = cita1.Equals(cita2);

            // Assert
            resultado.Should().BeTrue();
            (cita1 == cita2).Should().BeTrue();
        }

        [Test]
        public void GetHashCode_MismoId_MismoHashCode() {
            // Arrange
            var idComun = Guid.NewGuid();
            var cita1 = new Cita { Id = idComun, Dni = "11111111A" };
            var cita2 = new Cita { Id = idComun, Dni = "22222222B" };

            // Act
            var hash1 = cita1.GetHashCode();
            var hash2 = cita2.GetHashCode();

            // Assert
            hash1.Should().Be(hash2);
        }

        [Test]
        public void Record_MutacionConWith_DeberiaMantenerEstructuraInmutable() {
            // Arrange
            var citaOriginal = new Cita { Dni = "12345678A", Estado = EstadoCita.Pendiente };

            // Act
            var citaModificada = citaOriginal with { Estado = EstadoCita.Apta };

            // Assert
            citaModificada.Id.Should().Be(citaOriginal.Id);
            citaModificada.Dni.Should().Be("12345678A");
            citaModificada.Estado.Should().Be(EstadoCita.Apta);
        }
    }

    [TestFixture]
    public class CasosNegativos {

        [Test]
        public void Equals_Nulo_DeberiaRetornarFalse() {
            // Arrange
            var cita = new Cita();

            // Act
            var resultado = cita.Equals(null);

            // Assert
            resultado.Should().BeFalse();
        }

        [Test]
        public void Equals_DiferenteId_DeberiaSerDistinto() {
            // Arrange
            var cita1 = new Cita { Id = Guid.NewGuid() };
            var cita2 = new Cita { Id = Guid.NewGuid() };

            // Act
            var resultado = cita1.Equals(cita2);

            // Assert
            resultado.Should().BeFalse();
            (cita1 == cita2).Should().BeFalse();
        }
    }
}