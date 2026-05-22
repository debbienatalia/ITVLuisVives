using System;
using FluentAssertions;
using ITVLuisVives.Back.Cache;

namespace ITVLuisVives.Back.Tests.Cache;

[TestFixture]
public class AppCacheTests {
    
    [TestFixture]
    public class CasosPositivos {
        private AppCache<int, string> _cacheService = null!;

        [SetUp]
        public void SetUp() {
            _cacheService = new AppCache<int, string>(3);
        }

        [Test]
        public void Agregar_EntradaValida_DeberiaAlmacenarElValorCorrectamente() {
            // Act
            _cacheService.Add(10, "Seat Ibiza");

            // Assert
            _cacheService.Get(10).Should().Be("Seat Ibiza");
        }

        [Test]
        public void Agregar_VariosElementosSinLlenarCache_DeberianEstarTodosDisponibles() {
            // Act
            _cacheService.Add(1, "Renault Clio");
            _cacheService.Add(2, "Opel Corsa");
            _cacheService.Add(3, "Ford Fiesta");

            // Assert
            _cacheService.Get(1).Should().Be("Renault Clio");
            _cacheService.Get(2).Should().Be("Opel Corsa");
            _cacheService.Get(3).Should().Be("Ford Fiesta");
        }

        [Test]
        public void Agregar_SuperandoElLimiteDeCapacidad_DeberiaDesalojarElMenosUsado() {
            // Act
            _cacheService.Add(1, "Alfa");
            _cacheService.Add(2, "Bravo");
            _cacheService.Add(3, "Charlie");
            _cacheService.Add(4, "Delta"); 

            // Assert
            _cacheService.Get(1).Should().BeNull(); 
            _cacheService.Get(2).Should().Be("Bravo");
            _cacheService.Get(3).Should().Be("Charlie");
            _cacheService.Get(4).Should().Be("Delta");
        }

        [Test]
        public void Obtener_ElementoExistente_DeberiaRejuvenecerLaPrioridadEvitandoSuDesalojo() {
            // Arrange
            _cacheService.Add(1, "Alfa");
            _cacheService.Add(2, "Bravo");

            // Act
            _cacheService.Get(1); 
            _cacheService.Add(3, "Charlie");
            _cacheService.Add(4, "Delta"); 

            // Assert
            _cacheService.Get(2).Should().BeNull();
            _cacheService.Get(1).Should().Be("Alfa"); 
        }

        [Test]
        public void Eliminar_ClaveExistente_DeberiaRetornarTrueYBorrarElemento() {
            // Arrange
            _cacheService.Add(9, "Tesla Model 3");

            // Act
            var resultadoOperacion = _cacheService.Remove(9);

            // Assert
            resultadoOperacion.Should().BeTrue();
            _cacheService.Get(9).Should().BeNull();
        }

        [Test]
        public void Eliminar_ClaveInexistente_DeberiaRetornarFalse() {
            // Act
            var resultadoOperacion = _cacheService.Remove(999);

            // Assert
            resultadoOperacion.Should().BeFalse();
        }

        [Test]
        public void Obtener_ClaveInexistente_DeberiaRetornarDefault() {
            // Act
            var resultadoConsulta = _cacheService.Get(404);

            // Assert
            resultadoConsulta.Should().BeNull();
        }

        [Test]
        public void Agregar_ClaveYaExistente_DeberiaActualizarElValorYMantenerEntrada() {
            // Arrange
            _cacheService.Add(5, "Versión Antigua");

            // Act
            _cacheService.Add(5, "Versión Nueva");

            // Assert
            _cacheService.Get(5).Should().Be("Versión Nueva");
        }

        [Test]
        public void DisplayStatus_DeberiaEjecutarseSinErrores_CuandoSeInvoca() {
            // Arrange
            _cacheService.Add(1, "Prueba");

            // Act
            Action volcadoLogs = () => _cacheService.DisplayStatus();

            // Assert
            volcadoLogs.Should().NotThrow();
        }
    }

    [TestFixture]
    public class CasosNegativos {
        private AppCache<int, string?> _cacheServiceNullables = null!;

        [SetUp]
        public void SetUp() {
            _cacheServiceNullables = new AppCache<int, string?>(3);
        }

        [Test]
        public void Agregar_ValorNulo_DeberiaAdmitirseYGuardarNull() {
            // Act
            _cacheServiceNullables.Add(100, null);

            // Assert
            _cacheServiceNullables.Get(100).Should().BeNull();
        }

        [Test]
        public void Constructor_CapacidadCero_DeberiaLanzarExcepcionDeRango() {
            // Arrange & Act
            var construccionFallida = () => new AppCache<int, string>(0);

            // Assert
            construccionFallida.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Constructor_CapacidadNegativa_DeberiaLanzarExcepcionDeRango() {
            // Arrange & Act
            var construccionFallida = () => new AppCache<int, string>(-5);

            // Assert
            construccionFallida.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}