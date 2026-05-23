using System;
using System.Data;
using System.Linq;
using FluentAssertions;
using ITVLuisVives.Back.Entity;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Repositories;
using Microsoft.Data.Sqlite;

namespace ITVLuisVives.Test.Repositories;

[TestFixture]
public class CitaAdoRepositoryTests
{
    private SqliteConnection _connection = null!;
    private CitaAdoRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _repository = new CitaAdoRepository(_connection, dropData: true, seedData: false);
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Close();
        _connection.Dispose();
    }

    [Test]
    public void Constructor_SeedData_CargaDatos()
    {
        var repo = new CitaAdoRepository(_connection, dropData: true, seedData: true);
        repo.Count(includeDeleted: true).Should().BeGreaterThan(0);
    }

    [TestFixture]
    public class CasosPositivos : CitaAdoRepositoryTests
    {
        [Test]
        public void GetFiltered_TodosLosFiltros_RetornaCorrectamente()
        {
            var fecha = DateTime.Today;
            var c = _repository.Create(new Cita { Dni = "12345678A", VehiculoMatricula = "ABC1234", Estado = EstadoCita.Pendiente, FechaInspeccion = fecha, Observaciones = "Filtro" }).Value;

            var res = _repository.GetFiltered(
                dni: "12345678a",
                matricula: "abc",
                estado: EstadoCita.Pendiente,
                fechaDesde: fecha.AddDays(-1),
                fechaHasta: fecha.AddDays(1)
            );

            res.Should().HaveCount(1);
            res.First().Id.Should().Be(c.Id);
        }

        [Test]
        public void GetById_Existente_RetornaCita()
        {
            var c = _repository.Create(new Cita { Dni = "87654321B", VehiculoMatricula = "XYZ9876", Estado = EstadoCita.Pendiente, FechaInspeccion = DateTime.Today, Observaciones = "Id Test" }).Value;

            var res = _repository.GetById(c.Id);

            res.Should().NotBeNull();
            res!.Dni.Should().Be("87654321B");
        }

        [Test]
        public void Update_Correcto_RetornaExito()
        {
            var c = _repository.Create(new Cita { Dni = "11111111H", VehiculoMatricula = "M123", Estado = EstadoCita.Pendiente, FechaInspeccion = DateTime.Today, Observaciones = "Sin revisar" }).Value;

            var res = _repository.Update(c.Id, c with { Observaciones = "Revisión completada", Estado = EstadoCita.Apta });

            res.IsSuccess.Should().BeTrue();
            res.Value.Observaciones.Should().Be("Revisión completada");
            res.Value.Estado.Should().Be(EstadoCita.Apta);
        }

        [Test]
        public void Metodos_Especificos_De_Negocio_RetornanValoresCorrectos()
        {
            var fecha = DateTime.Today;
            _repository.Create(new Cita { Dni = "44445555K", VehiculoMatricula = "REG444", Estado = EstadoCita.Pendiente, FechaInspeccion = fecha, Observaciones = "Negocio" });

            _repository.ExisteCitaParaVehiculoEnFecha("reg444", fecha).Should().BeTrue();
            _repository.CountCitasPorDniYFecha("44445555k", fecha).Should().Be(1);
        }
    }

    [TestFixture]
    public class CasosNegativos : CitaAdoRepositoryTests
    {
        [Test]
        public void Create_ErrorEnDb_RetornaFailure()
        {
            _connection.Dispose();

            var res = _repository.Create(new Cita { Dni = "12345678A", VehiculoMatricula = "ERR", Estado = EstadoCita.Pendiente, FechaInspeccion = DateTime.Today });

            res.IsFailure.Should().BeTrue();
            res.Error.Code.Should().Be("DATABASE_ERROR");
        }

        [Test]
        public void Update_Inexistente_RetornaFailure()
        {
            var res = _repository.Update(Guid.NewGuid(), new Cita { Dni = "00000000Z", VehiculoMatricula = "NON", Estado = EstadoCita.Pendiente, FechaInspeccion = DateTime.Today });

            res.IsFailure.Should().BeTrue();
        }

        [Test]
        public void GetFiltered_Excepcion_RetornaVacio()
        {
            _connection.Dispose();

            var res = _repository.GetFiltered(null, null, null, null, null);

            res.Should().BeEmpty();
        }

        [Test]
        public void Delete_Inexistente_RetornaNull()
        {
            var res = _repository.Delete(Guid.NewGuid());
            res.Should().BeNull();
        }
    }

    [TestFixture]
    public class CasosMixtos : CitaAdoRepositoryTests
    {
        [Test]
        public void Paginacion_Calculos_Correctos()
        {
            for (int i = 0; i < 7; i++)
            {
                _repository.Create(new Cita { Dni = $"DNI{i}", VehiculoMatricula = $"MAT{i}", Estado = EstadoCita.Pendiente, FechaInspeccion = DateTime.Today, Observaciones = "Page" });
            }

            var res = _repository.GetFiltered(null, null, null, null, null, page: 2, pageSize: 5);

            res.Should().HaveCount(2);
        }

        [Test]
        public void BorradoLogico_Vs_Fisico()
        {
            var c = _repository.Create(new Cita { Dni = "12345678A", VehiculoMatricula = "DEL456", Estado = EstadoCita.Pendiente, FechaInspeccion = DateTime.Today, Observaciones = "Borrar" }).Value;

            _repository.Delete(c.Id, isLogical: true);
            _repository.Count(includeDeleted: false).Should().Be(0);
            _repository.Count(includeDeleted: true).Should().Be(1);

            var restoreRes = _repository.Restore(c.Id);
            restoreRes.IsSuccess.Should().BeTrue();
            _repository.Count(includeDeleted: false).Should().Be(1);

            _repository.Delete(c.Id, isLogical: false);
            _repository.GetById(c.Id).Should().BeNull();
            _repository.Count(includeDeleted: true).Should().Be(0);
        }

        [Test]
        public void Comportamiento_Excepciones_Globales()
        {
            _connection.Dispose();

            _repository.GetById(Guid.NewGuid()).Should().BeNull();
            _repository.ExisteCitaParaVehiculoEnFecha("MAT", DateTime.Today).Should().BeFalse();
            _repository.CountCitasPorDniYFecha("DNI", DateTime.Today).Should().Be(0);
            _repository.Update(Guid.NewGuid(), new Cita()).IsFailure.Should().BeTrue();
            _repository.Delete(Guid.NewGuid()).Should().BeNull();
            _repository.Restore(Guid.NewGuid()).IsFailure.Should().BeTrue();
            
            Assert.Throws<SqliteException>(() => _repository.Count());
        }
    }
}