using System;
using System.Data;
using System.Linq;
using Dapper;
using FluentAssertions;
using ITVLuisVives.Back.Entity;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Repositories;
using Microsoft.Data.Sqlite;

namespace ITVLuisVives.Test.Repositories;

[TestFixture]
public class CitaDapperRepositoryTests
{
    private SqliteConnection _connection = null!;
    private CitaDapperRepository _repository = null!;

    static CitaDapperRepositoryTests()
    {
        SqlMapper.AddTypeHandler(new GuidTypeHandler());
    }

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _repository = new CitaDapperRepository(_connection, dropData: true, seedData: false);
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
        var repo = new CitaDapperRepository(_connection, dropData: true, seedData: true);
        repo.Count(includeDeleted: true).Should().BeGreaterThan(0);
    }

    [TestFixture]
    public class CasosPositivos : CitaDapperRepositoryTests
    {
        [Test]
        public void GetFiltered_TodosLosFiltros_RetornaCorrectamente()
        {
            var fecha = DateTime.Today;
            _repository.Create(new Cita { Id = Guid.NewGuid(), Dni = "12345678A", VehiculoMatricula = "ABC1234", Estado = EstadoCita.Pendiente, FechaInspeccion = fecha, Observaciones = "Filtro" });

            var res = _repository.GetFiltered(
                dni: "12345678a",
                matricula: "abc",
                estado: EstadoCita.Pendiente,
                fechaDesde: fecha.AddDays(-1),
                fechaHasta: fecha.AddDays(1)
            );

            res.Should().HaveCount(1);
        }

        [Test]
        public void GetById_Existente_RetornaCita()
        {
            var id = Guid.NewGuid();
            _repository.Create(new Cita { Id = id, Dni = "87654321B", VehiculoMatricula = "XYZ9876", Estado = EstadoCita.Pendiente, Observaciones = "Id Test" });

            var res = _repository.GetById(id);

            res.Should().NotBeNull();
            res!.Dni.Should().Be("87654321B");
        }

        [Test]
        public void Update_Correcto_RetornaExito()
        {
            var c = _repository.Create(new Cita { Id = Guid.NewGuid(), Dni = "11111111H", VehiculoMatricula = "M123", Estado = EstadoCita.Pendiente, Observaciones = "Sin revisar" }).Value;

            var res = _repository.Update(c.Id, c with { Observaciones = "Revisión completada", Estado = EstadoCita.Apta });

            res.IsSuccess.Should().BeTrue();
            res.Value.Observaciones.Should().Be("Revisión completada");
            res.Value.Estado.Should().Be(EstadoCita.Apta);
        }
    }

    [TestFixture]
    public class CasosNegativos : CitaDapperRepositoryTests
    {
        [Test]
        public void Create_ErrorEnDb_RetornaFailure()
        {
            _connection.Close();

            var res = _repository.Create(new Cita { Id = Guid.NewGuid(), Dni = "12345678A", VehiculoMatricula = "ERR", Estado = EstadoCita.Pendiente, Observaciones = "Error" });

            res.IsFailure.Should().BeTrue();
            res.Error.Code.Should().Be("DATABASE_ERROR");
        }

        [Test]
        public void Update_Inexistente_RetornaFailure()
        {
            var res = _repository.Update(Guid.NewGuid(), new Cita { Id = Guid.NewGuid(), Dni = "00000000Z", VehiculoMatricula = "NON", Estado = EstadoCita.Pendiente, Observaciones = "Ninguna" });

            res.IsFailure.Should().BeTrue();
        }

        [Test]
        public void GetFiltered_Excepcion_RetornaVacio()
        {
            _connection.Close();

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
    public class CasosMixtos : CitaDapperRepositoryTests
    {
        [Test]
        public void Paginacion_Calculos_Correctos()
        {
            for (int i = 0; i < 7; i++)
            {
                _repository.Create(new Cita { Id = Guid.NewGuid(), Dni = $"DNI{i}", VehiculoMatricula = $"MAT{i}", Estado = EstadoCita.Pendiente, Observaciones = "Page" });
            }

            var res = _repository.GetFiltered(null, null, null, null, null, page: 2, pageSize: 5);

            res.Should().HaveCount(2);
        }

        [Test]
        public void BorradoLogico_Vs_Fisico()
        {
            var c = _repository.Create(new Cita { Id = Guid.NewGuid(), Dni = "12345678A", VehiculoMatricula = "DEL456", Estado = EstadoCita.Pendiente, Observaciones = "Borrar" }).Value;

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
        public void Metodos_Especificos_Y_Excepciones()
        {
            var fecha = DateTime.Today;
            _repository.Create(new Cita { Id = Guid.NewGuid(), Dni = "11111111A", VehiculoMatricula = "MATREG", Estado = EstadoCita.Pendiente, FechaInspeccion = fecha, Observaciones = "Negocio" });

            _repository.ExisteCitaParaVehiculoEnFecha("MATREG", fecha).Should().BeTrue();
            _repository.CountCitasPorDniYFecha("11111111A", fecha).Should().Be(1);

            _connection.Close();
            _repository.GetById(Guid.NewGuid()).Should().BeNull();
            _repository.ExisteCitaParaVehiculoEnFecha("MATREG", fecha).Should().BeFalse();
            _repository.CountCitasPorDniYFecha("11111111A", fecha).Should().Be(0);
            _repository.Update(Guid.NewGuid(), new Cita()).IsFailure.Should().BeTrue();
            _repository.Delete(Guid.NewGuid()).Should().BeNull();
            _repository.Restore(Guid.NewGuid()).IsFailure.Should().BeTrue();
            _repository.Count().Should().Be(0);
        }
    }

    private class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override void SetValue(IDbDataParameter parameter, Guid value)
        {
            parameter.Value = value.ToString();
        }

        public override Guid Parse(object value)
        {
            return Guid.Parse(value.ToString()!);
        }
    }
}