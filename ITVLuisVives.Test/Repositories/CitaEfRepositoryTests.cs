using FluentAssertions;
using ITVLuisVives.Back.Entity;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ITVLuisVives.Test.Repositories;

[TestFixture]
public class CitaEfRepositoryTests
{
    private SqliteConnection _connection = null!;
    private ItvDbContext _context = null!;
    private CitaEfRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<ItvDbContext>().UseSqlite(_connection).Options;
        _context = new ItvDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new CitaEfRepository(_context, dropData: true, seedData: false);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Test]
    public void Constructor_SeedData_CargaDatos()
    {
        var repo = new CitaEfRepository(_context, dropData: true, seedData: true);
        repo.Count().Should().BeGreaterThan(0);
    }

    [TestFixture]
    public class CasosPositivos : CitaEfRepositoryTests
    {
        [Test]
        public void GetFiltered_TodosLosFiltros_RetornaCorrectamente()
        {
            _repository.Create(new Cita { Dni = "12345678A", VehiculoMatricula = "ABC", Estado = EstadoCita.Pendiente, FechaInspeccion = DateTime.Today, Observaciones = "Check" });
            
            var res = _repository.GetFiltered("12345678a", "abc", EstadoCita.Pendiente, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));
            
            res.Should().HaveCount(1);
        }

        [Test]
        public void GetById_Existente_RetornaCita()
        {
            var c = _repository.Create(new Cita { Dni = "87654321B", VehiculoMatricula = "XYZ", Estado = EstadoCita.Pendiente, FechaInspeccion = DateTime.Today, Observaciones = "Check" }).Value;
            
            _repository.GetById(c.Id).Should().NotBeNull();
        }

        [Test]
        public void Update_Correcto_RetornaExito()
        {
            var c = _repository.Create(new Cita { Dni = "11111111H", VehiculoMatricula = "M1", Estado = EstadoCita.Pendiente, FechaInspeccion = DateTime.Today, Observaciones = "Original" }).Value;
            
            var res = _repository.Update(c.Id, c with { Observaciones = "Modificado" });
            
            res.IsSuccess.Should().BeTrue();
            res.Value.Observaciones.Should().Be("Modificado");
        }
    }

    [TestFixture]
    public class CasosNegativos : CitaEfRepositoryTests
    {
        [Test]
        public void Create_ErrorEnDb_RetornaFailure()
        {
            _connection.Close();
            
            var res = _repository.Create(new Cita { Dni = "12345678A", VehiculoMatricula = "ERR", Estado = EstadoCita.Pendiente, FechaInspeccion = DateTime.Today });
            
            res.IsFailure.Should().BeTrue();
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
            _connection.Close();
            
            _repository.GetFiltered(null, null, null, null, null).Should().BeEmpty();
        }

        [Test]
        public void Delete_Inexistente_RetornaNull()
        {
            _repository.Delete(Guid.NewGuid()).Should().BeNull();
        }
    }

    [TestFixture]
    public class CasosMixtos : CitaEfRepositoryTests
    {
        [Test]
        public void Paginacion_Calculos_Correctos()
        {
            for (int i = 0; i < 12; i++)
                _repository.Create(new Cita { Dni = $"DNI{i}", VehiculoMatricula = $"M{i}", Estado = EstadoCita.Pendiente, FechaInspeccion = DateTime.Today });
            
            _repository.GetFiltered(null, null, null, null, null, page: 3, pageSize: 5).Should().HaveCount(2);
        }

        [Test]
        public void BorradoLogico_Vs_Fisico()
        {
            var c = _repository.Create(new Cita { Dni = "12345678A", VehiculoMatricula = "D1", Estado = EstadoCita.Pendiente, FechaInspeccion = DateTime.Today }).Value;
            
            _repository.Delete(c.Id, isLogical: true);
            _repository.Count(false).Should().Be(0);
            
            _repository.Restore(c.Id).IsSuccess.Should().BeTrue();
            
            _repository.Delete(c.Id, isLogical: false);
            _repository.GetById(c.Id).Should().BeNull();
        }

        [Test]
        public void Metodos_Especificos_Y_Excepciones()
        {
            var fecha = DateTime.Today;
            _repository.Create(new Cita { Dni = "11111111A", VehiculoMatricula = "MAT1", Estado = EstadoCita.Pendiente, FechaInspeccion = fecha });
            
            _repository.ExisteCitaParaVehiculoEnFecha("MAT1", fecha).Should().BeTrue();
            _repository.CountCitasPorDniYFecha("11111111A", fecha).Should().Be(1);

            _connection.Close();
            _repository.GetById(Guid.NewGuid()).Should().BeNull();
            _repository.ExisteCitaParaVehiculoEnFecha("MAT1", fecha).Should().BeFalse();
            _repository.CountCitasPorDniYFecha("11111111A", fecha).Should().Be(0);
            _repository.Count().Should().Be(0);
            _repository.Restore(Guid.NewGuid()).IsFailure.Should().BeTrue();
        }
    }
}