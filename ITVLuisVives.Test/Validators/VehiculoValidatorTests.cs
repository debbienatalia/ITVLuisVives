using System;
using CSharpFunctionalExtensions;
using FluentAssertions;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Validators;

namespace ITVLuisVives.Test.Validators;

[TestFixture]
public class VehiculoValidatorTests
{
    [TestFixture]
    public class CasosPositivos
    {
        private VehiculoValidator _validador = null!;

        [SetUp]
        public void SetUp()
        {
            _validador = new VehiculoValidator();
        }

        [TestCase("1234BBB", "Seat", "Ibiza")]
        [TestCase("1234-BBB", "Renault", "Clio")]
        [TestCase("1234 bbb", "Peugeot", "208")]
        public void Validar_VehiculosValidos_DeberiaRetornarSuccess(string matricula, string marca, string modelo)
        {
            // Arrange
            var vehiculo = new Vehiculo
            {
                Matricula = matricula,
                Marca = marca,
                Modelo = modelo,
                Motor = TipoMotor.Gasolina,
                FechaMatriculacion = DateTime.Today.AddYears(-1)
            };

            // Act
            var resultado = _validador.Validar(vehiculo);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
        }

        [Test]
        public void Validar_MarcaEnElLimiteMaximo_DeberiaRetornarSuccess()
        {
            // Arrange
            var vehiculo = new Vehiculo
            {
                Matricula = "1234BBB",
                Marca = new string('A', 50),
                Modelo = "Leon",
                Motor = TipoMotor.Diesel,
                FechaMatriculacion = DateTime.Today
            };

            // Act
            var resultado = _validador.Validar(vehiculo);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Marca.Length.Should().Be(50);
        }
    }

    [TestFixture]
    public class CasosNegativos
    {
        private VehiculoValidator _validador = null!;

        [SetUp]
        public void SetUp()
        {
            _validador = new VehiculoValidator();
        }

        [Test]
        public void Validar_VehiculoNulo_DeberiaRetornarFailure()
        {
            // Act
            var resultado = _validador.Validar(null!);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Be("El objeto vehículo no puede ser nulo.");
        }

        [TestCase("")]
        [TestCase("123BBB")]
        [TestCase("12345BBB")]
        [TestCase("1234BB")]
        [TestCase("1234BBBB")]
        [TestCase("123A-BBB")]
        public void Validar_FormatosDeMatriculaIncorrectos_DeberiaRetornarFailure(string matriculaInvalida)
        {
            // Arrange
            var vehiculo = new Vehiculo
            {
                Matricula = matriculaInvalida,
                Marca = "Toyota",
                Modelo = "Yaris",
                Motor = TipoMotor.Híbrido,
                FechaMatriculacion = DateTime.Today
            };

            // Act
            var resultado = _validador.Validar(vehiculo);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Contain("La matrícula no es válida (4 números y 3 letras).");
        }
        
        [TestCase("A")]
        [TestCase("123456789012345678901234567890123456789012345678901")]
        public void Validar_MarcasFueraDeRango_DeberiaRetornarFailure(string marcaInvalida)
        {
            // Arrange
            var vehiculo = new Vehiculo
            {
                Matricula = "1234BBB",
                Marca = marcaInvalida,
                Modelo = "Yaris",
                Motor = TipoMotor.Híbrido,
                FechaMatriculacion = DateTime.Today
            };

            // Act
            var resultado = _validador.Validar(vehiculo);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Contain("La marca es obligatoria (2-50 caracteres.).");
        }

        [TestCase(-1)]
        [TestCase(999)]
        public void Validar_MotoresInexistentes_DeberiaRetornarFailure(int motorInvalido)
        {
            // Arrange
            var vehiculo = new Vehiculo
            {
                Matricula = "1234BBB",
                Marca = "Toyota",
                Modelo = "Yaris",
                Motor = (TipoMotor)motorInvalido,
                FechaMatriculacion = DateTime.Today
            };

            // Act
            var resultado = _validador.Validar(vehiculo);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Contain("El tipo de motor proporcionado no es válido.");
        }

        [Test]
        public void Validar_FechaMatriculacionFutura_DeberiaRetornarFailure()
        {
            // Arrange
            var vehiculo = new Vehiculo
            {
                Matricula = "1234BBB",
                Marca = "Ford",
                Modelo = "Fiesta",
                Motor = TipoMotor.Gasolina,
                FechaMatriculacion = DateTime.Today.AddDays(1)
            };

            // Act
            var resultado = _validador.Validar(vehiculo);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Contain("La fecha de matriculación no puede ser una fecha futura.");
        }

        [Test]
        public void Validar_MultiplesErroresSimultaneos_DeberiaAgruparlosEnElMensaje()
        {
            // Arrange
            var vehiculo = new Vehiculo
            {
                Matricula = "MALA",
                Marca = "",
                Modelo = "",
                Motor = TipoMotor.Gasolina,
                FechaMatriculacion = DateTime.Today
            };

            // Act
            var resultado = _validador.Validar(vehiculo);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Contain("La matrícula no es válida");
            resultado.Error.Message.Should().Contain("La marca es obligatoria");
            resultado.Error.Message.Should().Contain("El modelo es obligatorio");
        }
    }
}