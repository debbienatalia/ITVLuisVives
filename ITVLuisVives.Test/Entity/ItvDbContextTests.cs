using System;
using FluentAssertions;
using ITVLuisVives.Back.Entity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ITVLuisVives.Test.Entity;

[TestFixture]
public class ItvDbContextTests {

    [TestFixture]
    public class CasosPositivos {

        [Test]
        public void ConstructorConOpciones_DeberiaInicializarYExponerDbSets() {
            // Arrange
            var options = new DbContextOptionsBuilder<ItvDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

            // Act
            using var context = new ItvDbContext(options);

            // Assert
            context.Should().NotBeNull();
            context.Vehiculos.Should().NotBeNull();
            context.Citas.Should().NotBeNull();
        }

        [Test]
        public void ConstructorConConnectionString_DeberiaConfigurarSqliteYPermitirEnsureCreated() {
            // Arrange
            using var context = new ItvDbContext("Data Source=:memory:");
            
            // Act
            Action act = () => context.EnsureCreated();
            
            // Assert
            act.Should().NotThrow();
        }
    }

    [TestFixture]
    public class CasosNegativos {

        [Test]
        public void InstanciaSinProveedorConfigurado_AlRealizarConsultas_DeberiaLanzarExcepcion() {
            // Arrange
            var options = new DbContextOptionsBuilder<ItvDbContext>().Options;
            using var context = new ItvDbContext(options);

            // Act
            Action act = () => { var _ = context.Vehiculos.Count(); };

            // Assert
            act.Should().Throw<SqliteException>();
        }
    }
}