using FluentAssertions;
using ITVLuisVives.Back.Entity;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ITVLuisVives.Test.Repositories;

[TestFixture]
public class VehiculoEfRepositoryTests
{
    private SqliteConnection _connection = null!;
    private ItvDbContext _context = null!;
    private VehiculoEfRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<ItvDbContext>().UseSqlite(_connection).Options;
        _context = new ItvDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new VehiculoEfRepository(_context, dropData: true, seedData: false);
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
        var repo = new VehiculoEfRepository(_context, dropData: true, seedData: true);
        _repository.Count().Should().BeGreaterThan(0);
    }

    [TestFixture]
    public class CasosPositivos : VehiculoEfRepositoryTests
    {
        [Test]
        public void GetFiltered_TodosLosFiltros_RetornaCorrectamente()
        {
            _repository.Create(new Vehiculo { Matricula = "ABC", Marca = "Toyota", Motor = TipoMotor.Diesel, FechaMatriculacion = DateTime.Today });
            var res = _repository.GetFiltered("abc", "toy", TipoMotor.Diesel, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));
            res.Should().HaveCount(1);
        }

        [Test]
        public void GetByMatricula_Existente_RetornaVehiculo()
        {
            _repository.Create(new Vehiculo { Matricula = "XYZ", Marca = "Ford", Motor = TipoMotor.Gasolina, FechaMatriculacion = DateTime.Today });
            _repository.GetByMatricula("xyz").Should().NotBeNull();
        }

        [Test]
        public void Update_Correcto_RetornaExito()
        {
            var v = _repository.Create(new Vehiculo { Matricula = "A1", Marca = "M", Motor = TipoMotor.Gasolina, FechaMatriculacion = DateTime.Today }).Value;
            var res = _repository.Update(v.Id, v with { Marca = "Nuevo" });
            res.IsSuccess.Should().BeTrue();
            res.Value.Marca.Should().Be("Nuevo");
        }
    }

    [TestFixture]
    public class CasosNegativos : VehiculoEfRepositoryTests
    {
        [Test]
        public void Create_ErrorEnDb_RetornaFailure()
        {
            _connection.Close();
            var res = _repository.Create(new Vehiculo { Matricula = "ERR", Marca = "A", Motor = TipoMotor.Gasolina, FechaMatriculacion = DateTime.Today });
            res.IsFailure.Should().BeTrue();
        }

        [Test]
        public void Update_MatriculaDuplicada_RetornaFailure()
        {
            _repository.Create(new Vehiculo { Matricula = "AAA", Marca = "A", Motor = TipoMotor.Gasolina, FechaMatriculacion = DateTime.Today });
            var v2 = _repository.Create(new Vehiculo { Matricula = "BBB", Marca = "B", Motor = TipoMotor.Gasolina, FechaMatriculacion = DateTime.Today }).Value;
            var res = _repository.Update(v2.Id, new Vehiculo { Matricula = "AAA", Marca = "B", Motor = TipoMotor.Gasolina, FechaMatriculacion = DateTime.Today });
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
            _repository.Delete(999).Should().BeNull();
        }
    }

    [TestFixture]
    public class CasosMixtos : VehiculoEfRepositoryTests
    {
        [Test]
        public void Paginacion_Calculos_Correctos()
        {
            for (int i = 0; i < 12; i++)
                _repository.Create(new Vehiculo { Matricula = $"M{i}", Marca = "Marca", Motor = TipoMotor.Gasolina, FechaMatriculacion = DateTime.Today });
            
            _repository.GetFiltered(null, null, null, null, null, page: 3, pageSize: 5).Should().HaveCount(2);
        }

        [Test]
        public void BorradoLogico_Vs_Fisico()
        {
            var v = _repository.Create(new Vehiculo { Matricula = "D1", Marca = "A", Motor = TipoMotor.Gasolina, FechaMatriculacion = DateTime.Today }).Value;
            
            _repository.Delete(v.Id, isLogical: true);
            _repository.Count(false).Should().Be(0);
            
            _repository.Restore(v.Id).IsSuccess.Should().BeTrue();
            
            _repository.Delete(v.Id, isLogical: false);
            _repository.GetById(v.Id).Should().BeNull();
        }

        [Test]
        public void Excepciones_En_Varios_Metodos()
        {
            _connection.Close();
            _repository.GetById(1).Should().BeNull();
            _repository.ExisteMatricula("A").Should().BeFalse();
            _repository.Count().Should().Be(0);
            _repository.Restore(1).IsFailure.Should().BeTrue();
        }
    }
}