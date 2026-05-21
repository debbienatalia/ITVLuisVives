using System;
using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using FluentAssertions;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Repositories;
using ITVLuisVives.Back.Services.Citas;
using ITVLuisVives.Back.Validators;
using Moq;

namespace ITVLuisVives.Test.Services;

[TestFixture]
public class CitaServiceTests
{
    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<ICitaRepository>();
        _validatorMock = new Mock<IValidator<Cita>>();

        _validatorMock.Setup(v => v.Validar(It.IsAny<Cita>()))
            .Returns((Cita c) => Result.Success<Cita, DomainError>(c));

        _service = new CitaService(_repositoryMock.Object, _validatorMock.Object);
    }

    private CitaService _service = null!;
    private Mock<ICitaRepository> _repositoryMock = null!;
    private Mock<IValidator<Cita>> _validatorMock = null!;

    [TestFixture]
    public class CasosPositivos : CitaServiceTests
    {
        [Test]
        public void ObtenerFiltradas_ConDatosSucios_DeberiaFormatearYLLamarAlRepositorio()
        {
            // Arrange
            var fechaDesde = DateTime.Today;
            var fechaHasta = DateTime.Today.AddDays(7);
            _repositoryMock.Setup(r => r.GetFiltered("12345678Z", "1234BBB", EstadoCita.Pendiente, fechaDesde, fechaHasta, 2, 5, true))
                .Returns(new List<Cita> { new() });

            // Act
            var resultado = _service.ObtenerFiltradas(" 12345678 - Z ", " 1234 - BBB ", EstadoCita.Pendiente, fechaDesde, fechaHasta, 2, 5, true).ToList();

            // Assert
            resultado.Should().HaveCount(1);
            _repositoryMock.VerifyAll();
        }

        [Test]
        public void ObtenerFiltradas_ParametrosNull_DeberiaLlamarAlRepositorioConNull()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetFiltered(null, null, null, null, null, 1, 10, false))
                .Returns(new List<Cita>());

            // Act
            var resultado = _service.ObtenerFiltradas(null, null, null, null, null).ToList();

            // Assert
            resultado.Should().BeEmpty();
            _repositoryMock.VerifyAll();
        }

        [Test]
        public void ObtenerPorId_CitaExistente_DeberiaRetornarLaCita()
        {
            // Arrange
            var id = Guid.NewGuid();
            var cita = new Cita { Id = id };
            _repositoryMock.Setup(r => r.GetById(id)).Returns(cita);

            // Act
            var resultado = _service.ObtenerPorId(id);

            // Assert
            resultado.Should().NotBeNull();
            resultado!.Id.Should().Be(id);
        }

        [Test]
        public void Agendar_CitaValidaYCuposDisponibles_DeberiaGuardarExitosamente()
        {
            // Arrange
            var fecha = DateTime.Today.AddDays(2);
            var citaInput = new Cita { Dni = " 12345678 - A ", VehiculoMatricula = " 1234 - AAA ", FechaInspeccion = fecha };
            var citaEsperada = citaInput with { Dni = "12345678A", VehiculoMatricula = "1234AAA" };

            _repositoryMock.Setup(r => r.ExisteCitaParaVehiculoEnFecha("1234AAA", fecha.Date)).Returns(false);
            _repositoryMock.Setup(r => r.CountCitasPorDniYFecha("12345678A", fecha.Date)).Returns(0);
            _repositoryMock.Setup(r => r.Create(It.Is<Cita>(c => c.Dni == "12345678A" && c.VehiculoMatricula == "1234AAA")))
                .Returns(Result.Success<Cita, DomainError>(citaEsperada));

            // Act
            var resultado = _service.Agendar(citaInput);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Dni.Should().Be("12345678A");
            resultado.Value.VehiculoMatricula.Should().Be("1234AAA");
        }

        [Test]
        public void Actualizar_SinCambiosEnCamposCriticos_DeberiaActualizarDirectamente()
        {
            // Arrange
            var id = Guid.NewGuid();
            var fecha = DateTime.Today.AddDays(1);
            var existente = new Cita { Dni = "12345678Z", VehiculoMatricula = "1234BBB", FechaInspeccion = fecha };
            var modificada = new Cita { Dni = "12345678Z", VehiculoMatricula = "1234BBB", FechaInspeccion = fecha, Observaciones = "Solo cambia texto" };

            _repositoryMock.Setup(r => r.GetById(id)).Returns(existente);
            _repositoryMock.Setup(r => r.Update(id, It.IsAny<Cita>())).Returns(Result.Success<Cita, DomainError>(modificada));

            // Act
            var resultado = _service.Actualizar(id, modificada);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            _repositoryMock.Verify(r => r.ExisteCitaParaVehiculoEnFecha(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
            _repositoryMock.Verify(r => r.CountCitasPorDniYFecha(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Test]
        public void Actualizar_CambiaMatriculaAVehiculoLibre_DeberiaActualizar()
        {
            // Arrange
            var id = Guid.NewGuid();
            var fecha = DateTime.Today.AddDays(1);
            var existente = new Cita { Dni = "12345678Z", VehiculoMatricula = "1111AAA", FechaInspeccion = fecha };
            var modificada = new Cita { Dni = "12345678Z", VehiculoMatricula = "2222BBB", FechaInspeccion = fecha };

            _repositoryMock.Setup(r => r.GetById(id)).Returns(existente);
            _repositoryMock.Setup(r => r.ExisteCitaParaVehiculoEnFecha("2222BBB", fecha.Date)).Returns(false);
            _repositoryMock.Setup(r => r.Update(id, It.IsAny<Cita>())).Returns(Result.Success<Cita, DomainError>(modificada));

            // Act
            var resultado = _service.Actualizar(id, modificada);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            _repositoryMock.Verify(r => r.ExisteCitaParaVehiculoEnFecha("2222BBB", fecha.Date), Times.Once);
        }

        [Test]
        public void Actualizar_CambiaDniConCupoDisponible_DeberiaActualizar()
        {
            // Arrange
            var id = Guid.NewGuid();
            var fecha = DateTime.Today.AddDays(1);
            var existente = new Cita { Dni = "11111111A", VehiculoMatricula = "1234BBB", FechaInspeccion = fecha };
            var modificada = new Cita { Dni = "22222222B", VehiculoMatricula = "1234BBB", FechaInspeccion = fecha };

            _repositoryMock.Setup(r => r.GetById(id)).Returns(existente);
            _repositoryMock.Setup(r => r.CountCitasPorDniYFecha("22222222B", fecha.Date)).Returns(2); // Menor que 3 es válido
            _repositoryMock.Setup(r => r.Update(id, It.IsAny<Cita>())).Returns(Result.Success<Cita, DomainError>(modificada));

            // Act
            var resultado = _service.Actualizar(id, modificada);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            _repositoryMock.Verify(r => r.CountCitasPorDniYFecha("22222222B", fecha.Date), Times.Once);
        }

        [Test]
        public void Actualizar_CambiaFechaConMatriculaYDniValidos_DeberiaActualizar()
        {
            // Arrange
            var id = Guid.NewGuid();
            var fechaOriginal = DateTime.Today.AddDays(1);
            var fechaNueva = DateTime.Today.AddDays(2);
            var existente = new Cita { Dni = "12345678Z", VehiculoMatricula = "1234BBB", FechaInspeccion = fechaOriginal };
            var modificada = new Cita { Dni = "12345678Z", VehiculoMatricula = "1234BBB", FechaInspeccion = fechaNueva };

            _repositoryMock.Setup(r => r.GetById(id)).Returns(existente);
            _repositoryMock.Setup(r => r.ExisteCitaParaVehiculoEnFecha("1234BBB", fechaNueva.Date)).Returns(false);
            _repositoryMock.Setup(r => r.CountCitasPorDniYFecha("12345678Z", fechaNueva.Date)).Returns(1);
            _repositoryMock.Setup(r => r.Update(id, It.IsAny<Cita>())).Returns(Result.Success<Cita, DomainError>(modificada));

            // Act
            var resultado = _service.Actualizar(id, modificada);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
        }

        [Test]
        public void Cancelar_CitaExistente_DeberiaRetornarTrue()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.Delete(id, true)).Returns(new Cita());

            // Act
            var resultado = _service.Cancelar(id, true);

            // Assert
            resultado.Should().BeTrue();
        }

        [Test]
        public void Restaurar_DeberiaLlamarAlRepositorioYRetornarResultado()
        {
            // Arrange
            var id = Guid.NewGuid();
            var cita = new Cita { Id = id };
            _repositoryMock.Setup(r => r.Restore(id)).Returns(Result.Success<Cita, DomainError>(cita));

            // Act
            var resultado = _service.Restaurar(id);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
        }

        [Test]
        public void Contar_DeberiaRetornarElConteoDelRepositorio()
        {
            // Arrange
            _repositoryMock.Setup(r => r.Count(false)).Returns(8);

            // Act
            var resultado = _service.Contar(false);

            // Assert
            resultado.Should().Be(8);
        }
    }

    [TestFixture]
    public class CasosNegativos : CitaServiceTests
    {
        [Test]
        public void Constructor_RepositoryNull_DeberiaLanzarArgumentNullException()
        {
            // Act
            Action act = () => new CitaService(null!, _validatorMock.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("repository");
        }

        [Test]
        public void Constructor_ValidatorNull_DeberiaLanzarArgumentNullException()
        {
            // Act
            Action act = () => new CitaService(_repositoryMock.Object, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("validator");
        }

        [Test]
        public void Agendar_ValidacionFallida_DeberiaRetornarElErrorDelValidador()
        {
            // Arrange
            var cita = new Cita();
            var error = new DomainError("Error en formato", "VALIDATION_ERROR");
            _validatorMock.Setup(v => v.Validar(cita)).Returns(Result.Failure<Cita, DomainError>(error));

            // Act
            var resultado = _service.Agendar(cita);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("VALIDATION_ERROR");
        }

        [Test]
        public void Agendar_VehiculoYaTieneCitaEnFecha_DeberiaRetornarErrorVehicleBooked()
        {
            // Arrange
            var fecha = DateTime.Today.AddDays(1);
            var cita = new Cita { Dni = "12345678Z", VehiculoMatricula = "1234BBB", FechaInspeccion = fecha };
            _repositoryMock.Setup(r => r.ExisteCitaParaVehiculoEnFecha("1234BBB", fecha.Date)).Returns(true);

            // Act
            var resultado = _service.Agendar(cita);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("VEHICLE_ALREADY_BOOKED");
        }

        [Test]
        public void Agendar_DniAlcanzaLimiteDeCupos_DeberiaRetornarErrorMaxDniLimit()
        {
            // Arrange
            var fecha = DateTime.Today.AddDays(1);
            var cita = new Cita { Dni = "12345678Z", VehiculoMatricula = "1234BBB", FechaInspeccion = fecha };
            _repositoryMock.Setup(r => r.ExisteCitaParaVehiculoEnFecha("1234BBB", fecha.Date)).Returns(false);
            _repositoryMock.Setup(r => r.CountCitasPorDniYFecha("12345678Z", fecha.Date)).Returns(3); // Límite máximo alcanzado

            // Act
            var resultado = _service.Agendar(cita);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("MAX_DNI_LIMIT_EXCEEDED");
        }

        [Test]
        public void Actualizar_CitaNoExiste_DeberiaRetornarErrorNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetById(id)).Returns((Cita?)null);

            // Act
            var resultado = _service.Actualizar(id, new Cita());

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("NOT_FOUND");
        }

        [Test]
        public void Actualizar_ValidacionFallida_DeberiaRetornarElErrorDelValidador()
        {
            // Arrange
            var id = Guid.NewGuid();
            var existente = new Cita();
            var modif = new Cita();
            var error = new DomainError("Campos obligatorios vacíos", "VALIDATION_ERROR");

            _repositoryMock.Setup(r => r.GetById(id)).Returns(existente);
            _validatorMock.Setup(v => v.Validar(modif)).Returns(Result.Failure<Cita, DomainError>(error));

            // Act
            var resultado = _service.Actualizar(id, modif);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("VALIDATION_ERROR");
        }

        [Test]
        public void Actualizar_CambiaMatriculaAVehiculoOcupado_DeberiaRetornarErrorVehicleBooked()
        {
            // Arrange
            var id = Guid.NewGuid();
            var fecha = DateTime.Today.AddDays(1);
            var existente = new Cita { Dni = "12345678Z", VehiculoMatricula = "1111AAA", FechaInspeccion = fecha };
            var modificada = new Cita { Dni = "12345678Z", VehiculoMatricula = "2222BBB", FechaInspeccion = fecha };

            _repositoryMock.Setup(r => r.GetById(id)).Returns(existente);
            _repositoryMock.Setup(r => r.ExisteCitaParaVehiculoEnFecha("2222BBB", fecha.Date)).Returns(true);

            // Act
            var resultado = _service.Actualizar(id, modificada);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("VEHICLE_ALREADY_BOOKED");
        }

        [Test]
        public void Actualizar_CambiaFechaAVehiculoOcupado_DeberiaRetornarErrorVehicleBooked()
        {
            // Arrange
            var id = Guid.NewGuid();
            var fechaOriginal = DateTime.Today.AddDays(1);
            var fechaNueva = DateTime.Today.AddDays(2);
            var existente = new Cita { Dni = "12345678Z", VehiculoMatricula = "1234BBB", FechaInspeccion = fechaOriginal };
            var modificada = new Cita { Dni = "12345678Z", VehiculoMatricula = "1234BBB", FechaInspeccion = fechaNueva };

            _repositoryMock.Setup(r => r.GetById(id)).Returns(existente);
            _repositoryMock.Setup(r => r.ExisteCitaParaVehiculoEnFecha("1234BBB", fechaNueva.Date)).Returns(true);

            // Act
            var resultado = _service.Actualizar(id, modificada);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("VEHICLE_ALREADY_BOOKED");
        }

        [Test]
        public void Actualizar_CambiaDniYSuperaLimiteDeCupos_DeberiaRetornarErrorMaxDniLimit()
        {
            // Arrange
            var id = Guid.NewGuid();
            var fecha = DateTime.Today.AddDays(1);
            var existente = new Cita { Dni = "11111111A", VehiculoMatricula = "1234BBB", FechaInspeccion = fecha };
            var modificada = new Cita { Dni = "22222222B", VehiculoMatricula = "1234BBB", FechaInspeccion = fecha };

            _repositoryMock.Setup(r => r.GetById(id)).Returns(existente);
            _repositoryMock.Setup(r => r.CountCitasPorDniYFecha("22222222B", fecha.Date)).Returns(3);

            // Act
            var resultado = _service.Actualizar(id, modificada);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("MAX_DNI_LIMIT_EXCEEDED");
        }

        [Test]
        public void Actualizar_CambiaFechaYSuperaLimiteDeCuposParaElDni_DeberiaRetornarErrorMaxDniLimit()
        {
            // Arrange
            var id = Guid.NewGuid();
            var fechaOriginal = DateTime.Today.AddDays(1);
            var fechaNueva = DateTime.Today.AddDays(2);
            var existente = new Cita { Dni = "12345678Z", VehiculoMatricula = "1234BBB", FechaInspeccion = fechaOriginal };
            var modificada = new Cita { Dni = "12345678Z", VehiculoMatricula = "1234BBB", FechaInspeccion = fechaNueva };

            _repositoryMock.Setup(r => r.GetById(id)).Returns(existente);
            _repositoryMock.Setup(r => r.ExisteCitaParaVehiculoEnFecha("1234BBB", fechaNueva.Date)).Returns(false);
            _repositoryMock.Setup(r => r.CountCitasPorDniYFecha("12345678Z", fechaNueva.Date)).Returns(3);

            // Act
            var resultado = _service.Actualizar(id, modificada);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Code.Should().Be("MAX_DNI_LIMIT_EXCEEDED");
        }

        [Test]
        public void Cancelar_CitaNoExisteEnRepositorio_DeberiaRetornarFalse()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.Delete(id, true)).Returns((Cita?)null);

            // Act
            var resultado = _service.Cancelar(id, true);

            // Assert
            resultado.Should().BeFalse();
        }
    }
}