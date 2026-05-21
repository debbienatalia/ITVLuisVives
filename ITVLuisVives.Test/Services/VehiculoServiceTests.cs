using System;
using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using FluentAssertions;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Repositories;
using ITVLuisVives.Back.Services.Vehiculos;
using ITVLuisVives.Back.Validators;
using Moq;

namespace ITVLuisVives.Test.Services;

[TestFixture]
public class VehiculoServiceTests
{
    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IVehiculoRepository>();
        _validatorMock = new Mock<IValidator<Vehiculo>>();

        _validatorMock.Setup(v => v.Validar(It.IsAny<Vehiculo>()))
            .Returns((Vehiculo v) => Result.Success<Vehiculo, DomainError>(v));

        _service = new VehiculoService(_repositoryMock.Object, _validatorMock.Object);
    }

    private VehiculoService _service = null!;
    private Mock<IVehiculoRepository> _repositoryMock = null!;
    private Mock<IValidator<Vehiculo>> _validatorMock = null!;

    [TestFixture]
    public class CasosPositivos : VehiculoServiceTests
    {
        [Test]
        public void ObtenerFiltrados_ConMatriculaConEspacios_DeberiaLimpiarYLLamarAlRepositorio()
        {
            // Arrange
            var fechaDesde = DateTime.Today.AddMonths(-1);
            var fechaHasta = DateTime.Today;
            _repositoryMock.Setup(r => r.GetFiltered("1234BBB", "Seat", TipoMotor.Gasolina, fechaDesde, fechaHasta, 1, 10, false))
                .Returns(new List<Vehiculo> { new() { Matricula = "1234BBB" } });

            // Act
            var resultado = _service.ObtenerFiltrados("  1234bbb  ", "Seat", TipoMotor.Gasolina, fechaDesde, fechaHasta).ToList();

            // Assert
            resultado.Should().HaveCount(1);
            _repositoryMock.VerifyAll();
        }

        [Test]
        public void ObtenerFiltrados_MatriculaNull_DeberiaLlamarAlRepositorioConNull()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetFiltered(null, null, null, null, null, 1, 10, false))
                .Returns(new List<Vehiculo>());

            // Act
            var resultado = _service.ObtenerFiltrados(null, null, null, null, null).ToList();

            // Assert
            resultado.Should().BeEmpty();
            _repositoryMock.VerifyAll();
        }

        [Test]
        public void ObtenerPorId_VehiculoExistente_DeberiaRetornarElVehiculo()
        {
            // Arrange
            var vehiculo = new Vehiculo { Id = 42, Matricula = "1234BBB" };
            _repositoryMock.Setup(r => r.GetById(42)).Returns(vehiculo);

            // Act
            var resultado = _service.ObtenerPorId(42);

            // Assert
            resultado.Should().NotBeNull();
            resultado!.Id.Should().Be(42);
        }

        [Test]
        public void ObtenerPorMatricula_MatriculaValida_DeberiaRetornarElVehiculoLimpio()
        {
            // Arrange
            var vehiculo = new Vehiculo { Matricula = "1234BBB" };
            _repositoryMock.Setup(r => r.GetByMatricula("1234BBB")).Returns(vehiculo);

            // Act
            var resultado = _service.ObtenerPorMatricula("   1234bbb   ");

            // Assert
            resultado.Should().NotBeNull();
            resultado!.Matricula.Should().Be("1234BBB");
        }

        [Test]
        public void Registrar_VehiculoValidoYMatriculaUnica_DeberiaGuardarCorrectamente()
        {
            // Arrange
            var vehiculoInput = new Vehiculo { Matricula = "  1234bbb  ", Marca = "Toyota" };
            var vehiculoEsperado = vehiculoInput with { Matricula = "1234BBB" };

            _repositoryMock.Setup(r => r.ExisteMatricula("1234BBB")).Returns(false);
            _repositoryMock.Setup(r => r.Create(It.Is<Vehiculo>(v => v.Matricula == "1234BBB")))
                .Returns(Result.Success<Vehiculo, DomainError>(vehiculoEsperado));

            // Act
            var resultado = _service.Registrar(vehiculoInput);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Matricula.Should().Be("1234BBB");
            _repositoryMock.Verify(r => r.Create(It.IsAny<Vehiculo>()), Times.Once);
        }

        [Test]
        public void Actualizar_MismaMatriculaDiferenteCaso_DeberiaActualizarSinValidarDuplicados()
        {
            // Arrange
            var existente = new Vehiculo { Id = 1, Matricula = "1234BBB" };
            var modificado = new Vehiculo { Id = 1, Matricula = "1234bbb", Marca = "Audi" }; // Misma matrícula en minúsculas

            _repositoryMock.Setup(r => r.GetById(1)).Returns(existente);
            _repositoryMock.Setup(r => r.Update(1, It.Is<Vehiculo>(v => v.Matricula == "1234BBB")))
                .Returns(Result.Success<Vehiculo, DomainError>(modificado));

            // Act
            var resultado = _service.Actualizar(1, modificado);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            _repositoryMock.Verify(r => r.ExisteMatricula(It.IsAny<string>()), Times.Never); // No debe comprobar duplicados si es la suya
            _repositoryMock.Verify(r => r.Update(1, It.IsAny<Vehiculo>()), Times.Once);
        }

        [Test]
        public void Actualizar_NuevaMatriculaNoDuplicada_DeberiaActualizarCorrectamente()
        {
            // Arrange
            var existente = new Vehiculo { Id = 1, Matricula = "1111AAA" };
            var modificado = new Vehiculo { Id = 1, Matricula = "2222BBB" };

            _repositoryMock.Setup(r => r.GetById(1)).Returns(existente);
            _repositoryMock.Setup(r => r.ExisteMatricula("2222BBB")).Returns(false);
            _repositoryMock.Setup(r => r.Update(1, It.Is<Vehiculo>(v => v.Matricula == "2222BBB")))
                .Returns(Result.Success<Vehiculo, DomainError>(modificado));

            // Act
            var resultado = _service.Actualizar(1, modificado);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            _repositoryMock.Verify(r => r.ExisteMatricula("2222BBB"), Times.Once);
        }

        [Test]
        public void Eliminar_VehiculoExistente_DeberiaRetornarTrue()
        {
            // Arrange
            _repositoryMock.Setup(r => r.Delete(1, true)).Returns(new Vehiculo());

            // Act
            var resultado = _service.Eliminar(1, true);

            // Assert
            resultado.Should().BeTrue();
        }

        [Test]
        public void Restaurar_DeberiaLlamarAlRepositorioYRetornarResultado()
        {
            // Arrange
            var vehiculo = new Vehiculo { Id = 1 };
            _repositoryMock.Setup(r => r.Restore(1)).Returns(Result.Success<Vehiculo, DomainError>(vehiculo));

            // Act
            var resultado = _service.Restaurar(1);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            _repositoryMock.Verify(r => r.Restore(1), Times.Once);
        }

        [Test]
        public void Contar_DeberiaRetornarElConteoDelRepositorio()
        {
            // Arrange
            _repositoryMock.Setup(r => r.Count(true)).Returns(15);

            // Act
            var resultado = _service.Contar(true);

            // Assert
            resultado.Should().Be(15);
            _repositoryMock.Verify(r => r.Count(true), Times.Once);
        }
    }

    [TestFixture]
    public class CasosNegativos : VehiculoServiceTests
    {
        [Test]
        public void Constructor_RepositoryNull_DeberiaLanzarArgumentNullException()
        {
            // Act
            Action act = () => new VehiculoService(null!, _validatorMock.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("repository");
        }

        [Test]
        public void Constructor_ValidatorNull_DeberiaLanzarArgumentNullException()
        {
            // Act
            Action act = () => new VehiculoService(_repositoryMock.Object, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("validator");
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void ObtenerPorMatricula_MatriculaInvalida_DeberiaRetornarNullInmediatamente(string? matriculaInvalida)
        {
            // Act
            var resultado = _service.ObtenerPorMatricula(matriculaInvalida!);

            // Assert
            resultado.Should().BeNull();
            _repositoryMock.Verify(r => r.GetByMatricula(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Registrar_ValidacionFallida_DeberiaRetornarElErrorDelValidador()
        {
            // Arrange
            var vehiculo = new Vehiculo { Matricula = "1234BBB" };
            var errorValidacion = new DomainError("Matrícula inválida por formato", "VALIDATION_ERROR");
            
            _validatorMock.Setup(v => v.Validar(vehiculo))
                .Returns(Result.Failure<Vehiculo, DomainError>(errorValidacion));

            // Act
            var resultado = _service.Registrar(vehiculo);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("VALIDATION_ERROR");
            _repositoryMock.Verify(r => r.ExisteMatricula(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Registrar_MatriculaDuplicada_DeberiaRetornarErrorDuplicate()
        {
            // Arrange
            var vehiculo = new Vehiculo { Matricula = "1234BBB" };
            _repositoryMock.Setup(r => r.ExisteMatricula("1234BBB")).Returns(true);

            // Act
            var resultado = _service.Registrar(vehiculo);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("DUPLICATE_MATRICULA");
            _repositoryMock.Verify(r => r.Create(It.IsAny<Vehiculo>()), Times.Never);
        }

        [Test]
        public void Actualizar_VehiculoNoExiste_DeberiaRetornarErrorNotFound()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetById(999)).Returns((Vehiculo?)null);

            // Act
            var resultado = _service.Actualizar(999, new Vehiculo());

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("NOT_FOUND");
            _repositoryMock.Verify(r => r.Update(It.IsAny<int>(), It.IsAny<Vehiculo>()), Times.Never);
        }

        [Test]
        public void Actualizar_ValidacionFallida_DeberiaRetornarElErrorDelValidador()
        {
            // Arrange
            var existente = new Vehiculo { Id = 1, Matricula = "1234BBB" };
            var modificado = new Vehiculo { Id = 1, Matricula = "1234BBB" };
            var errorValidacion = new DomainError("Error de negocio", "VALIDATION_ERROR");

            _repositoryMock.Setup(r => r.GetById(1)).Returns(existente);
            _validatorMock.Setup(v => v.Validar(modificado))
                .Returns(Result.Failure<Vehiculo, DomainError>(errorValidacion));

            // Act
            var resultado = _service.Actualizar(1, modificado);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("VALIDATION_ERROR");
        }

        [Test]
        public void Actualizar_CambiaMatriculaAUnVehiculoQueYaExiste_DeberiaRetornarErrorDuplicate()
        {
            // Arrange
            var existente = new Vehiculo { Id = 1, Matricula = "1111AAA" };
            var modificado = new Vehiculo { Id = 1, Matricula = "2222BBB" };

            _repositoryMock.Setup(r => r.GetById(1)).Returns(existente);
            _repositoryMock.Setup(r => r.ExisteMatricula("2222BBB")).Returns(true);

            // Act
            var resultado = _service.Actualizar(1, modificado);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("DUPLICATE_MATRICULA");
            _repositoryMock.Verify(r => r.Update(It.IsAny<int>(), It.IsAny<Vehiculo>()), Times.Never);
        }

        [Test]
        public void Eliminar_VehiculoNoExisteEnRepositorio_DeberiaRetornarFalse()
        {
            // Arrange
            _repositoryMock.Setup(r => r.Delete(999, true)).Returns((Vehiculo?)null);

            // Act
            var resultado = _service.Eliminar(999, true);

            // Assert
            resultado.Should().BeFalse();
        }
    }
}