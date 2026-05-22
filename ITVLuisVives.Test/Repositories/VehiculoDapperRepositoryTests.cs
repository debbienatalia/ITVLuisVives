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
public class VehiculoDapperRepositoryTests
{
    private SqliteConnection _connection = null!;
    private VehiculoDapperRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _repository = new VehiculoDapperRepository(_connection, dropData: true, seedData: false);
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
        var repo = new VehiculoDapperRepository(_connection, dropData: true, seedData: true);
        repo.Count(includeDeleted: true).Should().BeGreaterThan(0);
    }

    [TestFixture]
    public class CasosPositivos : VehiculoDapperRepositoryTests
    {
        [Test]
        public void GetFiltered_TodosLosFiltros_RetornaCorrectamente()
        {
            var fecha = DateTime.Today;
            _repository.Create(new Vehiculo { Matricula = "1234ABC", Marca = "Seat", Modelo = "Ibiza", Motor = TipoMotor.Gasolina, FechaMatriculacion = fecha });

            var res = _repository.GetFiltered(
                matricula: "abc", 
                marca: "sea", 
                motor: TipoMotor.Gasolina, 
                matriculacionDesde: fecha.AddDays(-1), 
                matriculacionHasta: fecha.AddDays(1)
            );

            res.Should().HaveCount(1);
        }

        [Test]
        public void GetByMatricula_Existente_RetornaVehiculo()
        {
            _repository.Create(new Vehiculo { Matricula = "XYZ999", Marca = "Audi", Modelo = "A4", Motor = TipoMotor.Diesel, FechaMatriculacion = DateTime.Today });
            
            var res = _repository.GetByMatricula("xyz999");
            
            res.Should().NotBeNull();
            res!.Marca.Should().Be("Audi");
        }

        [Test]
        public void Update_Correcto_RetornaExito()
        {
            var v = _repository.Create(new Vehiculo { Matricula = "M410", Marca = "Renault", Modelo = "Clio", Motor = TipoMotor.Eléctrico, FechaMatriculacion = DateTime.Today }).Value;
            
            var res = _repository.Update(v.Id, v with { Modelo = "Zoe" });
            
            res.IsSuccess.Should().BeTrue();
            res.Value.Modelo.Should().Be("Zoe");
        }
    }

    [TestFixture]
    public class CasosNegativos : VehiculoDapperRepositoryTests
    {
        [Test]
        public void Create_ErrorEnDb_RetornaFailure()
        {
            _connection.Close();
            
            var res = _repository.Create(new Vehiculo { Matricula = "ERR", Marca = "A", Modelo = "B", Motor = TipoMotor.Gasolina, FechaMatriculacion = DateTime.Today });
            
            res.IsFailure.Should().BeTrue();
            res.Error.Code.Should().Be("DATABASE_ERROR");
        }

        [Test]
        public void Update_MatriculaDuplicada_RetornaFailure()
        {
            _repository.Create(new Vehiculo { Matricula = "AAA111", Marca = "A", Modelo = "X", Motor = TipoMotor.Gasolina, FechaMatriculacion = DateTime.Today });
            var v2 = _repository.Create(new Vehiculo { Matricula = "BBB222", Marca = "B", Modelo = "Y", Motor = TipoMotor.Gasolina, FechaMatriculacion = DateTime.Today }).Value;
            
            var res = _repository.Update(v2.Id, v2 with { Matricula = "AAA111" });
            
            res.IsFailure.Should().BeTrue();
            res.Error.Code.Should().Be("MATRICULA_DUPLICADA");
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
            var res = _repository.Delete(9999);
            res.Should().BeNull();
        }
    }

    [TestFixture]
    public class CasosMixtos : VehiculoDapperRepositoryTests
    {
        [Test]
        public void Paginacion_Calculos_Correctos()
        {
            for (int i = 0; i < 7; i++)
            {
                _repository.Create(new Vehiculo { Matricula = $"MAT{i}", Marca = "Test", Modelo = "Test", Motor = TipoMotor.Gasolina, FechaMatriculacion = DateTime.Today });
            }

            var res = _repository.GetFiltered(null, null, null, null, null, page: 2, pageSize: 5);
            
            res.Should().HaveCount(2);
        }

        [Test]
        public void BorradoLogico_Vs_Fisico()
        {
            var v = _repository.Create(new Vehiculo { Matricula = "DEL123", Marca = "Mazda", Modelo = "3", Motor = TipoMotor.Gasolina, FechaMatriculacion = DateTime.Today }).Value;

            _repository.Delete(v.Id, isLogical: true);
            _repository.Count(includeDeleted: false).Should().Be(0);
            _repository.Count(includeDeleted: true).Should().Be(1);

            var restoreRes = _repository.Restore(v.Id);
            restoreRes.IsSuccess.Should().BeTrue();
            _repository.Count(includeDeleted: false).Should().Be(1);

            _repository.Delete(v.Id, isLogical: false);
            _repository.GetById(v.Id).Should().BeNull();
            _repository.Count(includeDeleted: true).Should().Be(0);
        }

        [Test]
        public void Excepciones_En_Varios_Metodos()
        {
            _connection.Close();

            _repository.GetById(1).Should().BeNull();
            _repository.GetByMatricula("A").Should().BeNull();
            _repository.ExisteMatricula("A").Should().BeFalse();
            _repository.Update(1, new Vehiculo()).IsFailure.Should().BeTrue();
            _repository.Delete(1).Should().BeNull();
            _repository.Restore(1).IsFailure.Should().BeTrue();
            _repository.Count().Should().Be(0);
        }
    }
}