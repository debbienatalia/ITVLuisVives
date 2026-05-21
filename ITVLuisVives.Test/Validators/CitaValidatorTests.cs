using System;
using CSharpFunctionalExtensions;
using FluentAssertions;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Validators;

namespace ITVLuisVives.Test.Validators;

[TestFixture]
public class CitaValidatorTests
{
    [TestFixture]
    public class CasosPositivos
    {
        private CitaValidator _validador = null!;

        [SetUp]
        public void SetUp()
        {
            _validador = new CitaValidator();
        }

        [TestCase("12345678Z", "1234BBB")]
        [TestCase("00000001R", "1234-BBB")]
        [TestCase("87654321X", "1234 BBB")]
        [TestCase(" 12345678-Z ", "  1234BBB  ")]
        public void Validar_CitaValida_DeberiaRetornarSuccess(string dni, string matricula)
        {
            // Arrange
            var cita = new Cita
            {
                Dni = dni,
                VehiculoMatricula = matricula,
                FechaInspeccion = DateTime.Today.AddDays(5),
                Estado = default(EstadoCita),
                Observaciones = "Todo correcto"
            };

            // Act
            var resultado = _validador.Validar(cita);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().NotBeNull();
        }

        [TestCase(0)]
        [TestCase(15)]
        [TestCase(30)]
        public void Validar_FechasLimitesValidas_DeberiaRetornarSuccess(int diasAdicionales)
        {
            // Arrange
            var cita = new Cita
            {
                Dni = "12345678Z",
                VehiculoMatricula = "1234BBB",
                FechaInspeccion = DateTime.Today.AddDays(diasAdicionales),
                Estado = default(EstadoCita),
                Observaciones = null
            };

            // Act
            var resultado = _validador.Validar(cita);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
        }
    }

    [TestFixture]
    public class CasosNegativos
    {
        private CitaValidator _validador = null!;

        [SetUp]
        public void SetUp()
        {
            _validador = new CitaValidator();
        }

        [Test]
        public void Validar_CitaNula_DeberiaRetornarFailure()
        {
            // Act
            var resultado = _validador.Validar(null!);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Be("El objeto cita no puede ser nulo.");
        }
        
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("1234567A")]
        [TestCase("123456789A")]
        [TestCase("12345678")]
        [TestCase("ABCDEFGHZ")]
        public void Validar_FormatosDeDniIncorrectos_DeberiaRetornarFailure(string dniInvalido)
        {
            // Arrange
            var cita = new Cita
            {
                Dni = dniInvalido,
                VehiculoMatricula = "1234BBB",
                FechaInspeccion = DateTime.Today,
                Estado = default(EstadoCita)
            };

            // Act
            var resultado = _validador.Validar(cita);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Contain("El DNI no tiene un formato válido (8 números y 1 letra).");
        }

        [TestCase("12345678A")]
        [TestCase("00000001T")]
        public void Validar_DniConLetraDeControlInvalida_DeberiaRetornarFailure(string dniLetraErronea)
        {
            // Arrange
            var cita = new Cita
            {
                Dni = dniLetraErronea,
                VehiculoMatricula = "1234BBB",
                FechaInspeccion = DateTime.Today,
                Estado = default(EstadoCita)
            };

            // Act
            var resultado = _validador.Validar(cita);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Contain("La letra del DNI no coincide con el dígito de control oficial.");
        }

        [TestCase("")]
        [TestCase("123BBB")]
        [TestCase("12345BBB")]
        [TestCase("1234BB")]
        [TestCase("1234BBBB")]
        public void Validar_FormatosDeMatriculaIncorrectos_DeberiaRetornarFailure(string matriculaInvalida)
        {
            // Arrange
            var cita = new Cita
            {
                Dni = "12345678Z",
                VehiculoMatricula = matriculaInvalida,
                FechaInspeccion = DateTime.Today,
                Estado = default(EstadoCita)
            };

            // Act
            var resultado = _validador.Validar(cita);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Contain("La matrícula del vehículo no es válida (4 números y 3 letras).");
        }

        [TestCase(-1)]
        [TestCase(31)]
        public void Validar_FechasFueraDeRango_DeberiaRetornarFailure(int diasDesfase)
        {
            // Arrange
            var cita = new Cita
            {
                Dni = "12345678Z",
                VehiculoMatricula = "1234BBB",
                FechaInspeccion = DateTime.Today.AddDays(diasDesfase),
                Estado = default(EstadoCita)
            };

            // Act
            var resultado = _validador.Validar(cita);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Contain("La fecha de inspección debe programarse entre el día de hoy y los próximos 30 días naturales.");
        }

        [Test]
        public void Validar_FechaInspeccionDefault_DeberiaRetornarFailure()
        {
            // Arrange
            var cita = new Cita
            {
                Dni = "12345678Z",
                VehiculoMatricula = "1234BBB",
                FechaInspeccion = default,
                Estado = default(EstadoCita)
            };

            // Act
            var resultado = _validador.Validar(cita);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Contain("La fecha de inspección es obligatoria.");
        }

        [TestCase(-1)]
        [TestCase(99)]
        public void Validar_EstadoInvalido_DeberiaRetornarFailure(int estadoIncorrecto)
        {
            // Arrange
            var cita = new Cita
            {
                Dni = "12345678Z",
                VehiculoMatricula = "1234BBB",
                FechaInspeccion = DateTime.Today,
                Estado = (EstadoCita)estadoIncorrecto
            };

            // Act
            var resultado = _validador.Validar(cita);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Contain("El estado asignado a la cita no es un estado válido del sistema.");
        }

        [Test]
        public void Validar_ObservacionesDemasiadoLargas_DeberiaRetornarFailure()
        {
            // Arrange
            var cita = new Cita
            {
                Dni = "12345678Z",
                VehiculoMatricula = "1234BBB",
                FechaInspeccion = DateTime.Today,
                Estado = default(EstadoCita),
                Observaciones = new string('X', 501)
            };

            // Act
            var resultado = _validador.Validar(cita);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Contain("Las observaciones no pueden superar los 500 caracteres.");
        }

        [Test]
        public void Validar_MultiplesErroresSimultaneos_DeberiaAcumularMensajes()
        {
            // Arrange
            var cita = new Cita
            {
                Dni = "INVALIDO",
                VehiculoMatricula = "MAL",
                FechaInspeccion = default,
                Estado = (EstadoCita)99
            };

            // Act
            var resultado = _validador.Validar(cita);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Contain("El DNI no tiene un formato válido");
            resultado.Error.Message.Should().Contain("La matrícula del vehículo no es válida");
            resultado.Error.Message.Should().Contain("La fecha de inspección es obligatoria");
            resultado.Error.Message.Should().Contain("El estado asignado a la cita no es un estado válido");
        }
    }
}