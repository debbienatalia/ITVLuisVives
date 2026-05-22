using FluentAssertions;
using ITVLuisVives.Back.Errors;

namespace ITVLuisVives.Test.Errors;

[TestFixture]
public class DomainErrorTests {

    [TestFixture]
    public class CasosPositivos {

        [Test]
        public void Constructor_DeberiaAsignarMensajeYCodigoCorrectamente() {
            // Act
            var error = new DomainError("Error de validación en matrícula", "INVALID_MATRICULA");

            // Assert
            error.Message.Should().Be("Error de validación en matrícula");
            error.Code.Should().Be("INVALID_MATRICULA");
        }

        [Test]
        public void Constructor_SinCodigo_DeberiaAsignarNullPorDefecto() {
            // Act
            var error = new DomainError("Error inesperado del sistema");

            // Assert
            error.Message.Should().Be("Error inesperado del sistema");
            error.Code.Should().BeNull();
        }

        [Test]
        public void NotFound_DeberiaFormatearMensajeYCodigoCorrectamente() {
            // Act
            var error = DomainError.NotFound("Cita", "guid-ficticio-123");

            // Assert
            error.Code.Should().Be("NOT_FOUND");
            error.Message.Should().Be("No se encontró la entidad 'Cita' con el identificador: guid-ficticio-123.");
        }

        [Test]
        public void AlreadyExists_DeberiaAsignarMensajeYCodigoCorrectamente() {
            // Act
            var error = DomainError.AlreadyExists("El vehículo con esta matrícula ya existe en el sistema.");

            // Assert
            error.Code.Should().Be("ALREADY_EXISTS");
            error.Message.Should().Be("El vehículo con esta matrícula ya existe en el sistema.");
        }

        [Test]
        public void Record_Equality_MismosValores_DeberianSerIguales() {
            // Arrange
            var error1 = new DomainError("Error base", "CODE1");
            var error2 = new DomainError("Error base", "CODE1");

            // Act & Assert
            (error1 == error2).Should().BeTrue();
            error1.Equals(error2).Should().BeTrue();
            error1.GetHashCode().Should().Be(error2.GetHashCode());
        }
    }

    [TestFixture]
    public class CasosNegativos {

        [Test]
        public void Equals_Nulo_DeberiaRetornarFalse() {
            // Arrange
            var error = new DomainError("Error de negocio");

            // Act
            var resultado = error.Equals(null);

            // Assert
            resultado.Should().BeFalse();
        }

        [Test]
        public void Equals_DiferentesValores_DeberiaRetornarFalse() {
            // Arrange
            var error1 = new DomainError("Mensaje A", "CODE");
            var error2 = new DomainError("Mensaje B", "CODE");

            // Act & Assert
            (error1 == error2).Should().BeFalse();
        }
    }
}