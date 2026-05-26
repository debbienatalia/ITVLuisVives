using System;
using System.IO;
using FluentAssertions;
using ITVLuisVives.Back.Cache;
using ITVLuisVives.Back.Factories;
using ITVLuisVives.Back.Infrastructure;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Repositories;
using ITVLuisVives.Back.Services;
using ITVLuisVives.Back.Validators;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace ITVLuisVives.Back.Test.Infrastructure;

[TestFixture]
public class DependenciesProviderTests
{
    [TestFixture]
    public class CasosPositivos
    {
        private ServiceProvider? _provider;

        [TearDown]
        public void TearDown()
        {
            _provider?.Dispose();
            _provider = null;

            SqliteConnection.ClearAllPools();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        [Test]
        public void BuildServiceProvider_DeberiaResolverICitaService()
        {
            _provider = (ServiceProvider)DependenciesProvider.BuildServiceProvider();
            var service = _provider.GetService<ICitaService>();
            service.Should().NotBeNull();
        }

        [Test]
        public void BuildServiceProvider_DeberiaResolverValidator()
        {
            _provider = (ServiceProvider)DependenciesProvider.BuildServiceProvider();
            var validator = _provider.GetService<IValidator<Cita>>();
            validator.Should().NotBeNull();
        }

        [Test]
        public void BuildServiceProvider_DeberiaResolverStorageFactory()
        {
            _provider = (ServiceProvider)DependenciesProvider.BuildServiceProvider();
            var factory = _provider.GetService<IStorageFactory>();
            factory.Should().NotBeNull();
        }

        [Test]
        public void BuildServiceProvider_DeberiaResolverImportExportService()
        {
            _provider = (ServiceProvider)DependenciesProvider.BuildServiceProvider();
            var service = _provider.GetService<IImportExportService>();
            service.Should().NotBeNull();
        }

        [Test]
        public void BuildServiceProvider_DeberiaResolverReportService()
        {
            _provider = (ServiceProvider)DependenciesProvider.BuildServiceProvider();
            var reportService = _provider.GetService<IReportService>();
            reportService.Should().NotBeNull();
        }

        [Test]
        public void BuildServiceProvider_DeberiaResolverCaches()
        {
            _provider = (ServiceProvider)DependenciesProvider.BuildServiceProvider();
            var cacheGuid = _provider.GetService<ICache<Guid, Cita>>();
            var cacheString = _provider.GetService<ICache<string, Cita>>();

            cacheGuid.Should().NotBeNull();
            cacheString.Should().NotBeNull();
        }

        [Test]
        public void BuildServiceProvider_DeberiaResolverRepositorio()
        {
            _provider = (ServiceProvider)DependenciesProvider.BuildServiceProvider();
            var repo = _provider.GetService<ICitaRepository>();
            repo.Should().NotBeNull();
        }

        [Test]
        public void BuildServiceProvider_DeberiaAceptarDependenciasAdicionales()
        {
            _provider = (ServiceProvider)DependenciesProvider.BuildServiceProvider(services =>
            {
                services.AddSingleton("TEST_OK");
            });

            var value = _provider.GetService<string>();
            value.Should().Be("TEST_OK");
        }
    }

    [TestFixture]
    public class CasosNegativos
    {
        private ServiceProvider? _provider;

        [TearDown]
        public void TearDown()
        {
            _provider?.Dispose();
            _provider = null;

            SqliteConnection.ClearAllPools();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        [Test]
        public void GetService_DeServicioNoRegistrado_DeberiaRetornarNull()
        {
            _provider = (ServiceProvider)DependenciesProvider.BuildServiceProvider();
            var service = _provider.GetService<Random>();
            service.Should().BeNull();
        }

        [Test]
        public void BuildServiceProvider_NoDeberiaLanzarExcepcion()
        {
            Action action = () =>
            {
                _provider = (ServiceProvider)DependenciesProvider.BuildServiceProvider();
            };
            action.Should().NotThrow();
        }
    }

    [TestFixture]
    public class CasosMixtos
    {
        private ServiceProvider? _provider;

        [TearDown]
        public void TearDown()
        {
            _provider?.Dispose();
            _provider = null;

            SqliteConnection.ClearAllPools();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        [Test]
        public void BuildServiceProvider_DeberiaMantenerSingletons()
        {
            _provider = (ServiceProvider)DependenciesProvider.BuildServiceProvider();
            var cache1 = _provider.GetRequiredService<ICache<Guid, Cita>>();
            var cache2 = _provider.GetRequiredService<ICache<Guid, Cita>>();

            cache1.Should().BeSameAs(cache2);
        }

        [Test]
        public void BuildServiceProvider_DeberiaCrearScopedServicesDiferentes()
        {
            _provider = (ServiceProvider)DependenciesProvider.BuildServiceProvider();
            using var scope1 = _provider.CreateScope();
            using var scope2 = _provider.CreateScope();

            var service1 = scope1.ServiceProvider.GetRequiredService<ICitaService>();
            var service2 = scope2.ServiceProvider.GetRequiredService<ICitaService>();

            service1.Should().NotBeSameAs(service2);
        }

        [Test]
        public void BuildServiceProvider_DeberiaResolverServiciosMultiplesVeces()
        {
            _provider = (ServiceProvider)DependenciesProvider.BuildServiceProvider();
            var repo1 = _provider.GetService<ICitaRepository>();
            var repo2 = _provider.GetService<ICitaRepository>();

            repo1.Should().NotBeNull();
            repo2.Should().NotBeNull();
        }
    }
}