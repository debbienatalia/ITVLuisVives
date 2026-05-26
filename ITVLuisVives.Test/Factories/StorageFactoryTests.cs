using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using ITVLuisVives.Back.Factories;
using ITVLuisVives.Back.Storage;
using ITVLuisVives.Back.Config;
using FluentAssertions;

namespace ITVLuisVives.Test.Factories;

[TestFixture]
public class StorageFactoryTests
{
    [SetUp]
    public void SetUp()
    {
        _factory = new StorageFactory();
    }

    private StorageFactory _factory = null!;

    [TestFixture]
    public class CasosPositivos
    {
        [SetUp]
        public void SetUp()
        {
            _factory = new StorageFactory();
        }

        private StorageFactory _factory = null!;

        [TestCase("citas.json", typeof(CitaJsonStorage))]
        [TestCase("CITAS.JSON", typeof(CitaJsonStorage))]
        [TestCase("C:\\datos\\respaldo.xml", typeof(CitaXmlStorage))]
        [TestCase("/home/user/exportacion.csv", typeof(CitaCsvStorage))]
        public void Crear_ConExtensionValida_DeberiaRetornarStorageCorrecto(string path, Type tipoEsperado)
        {
            // Act
            var resultado = _factory.Crear(path);

            // Assert
            resultado.Should().NotBeNull();
            resultado.Should().BeOfType(tipoEsperado);
        }
    }

    [TestFixture]
    public class CasosNegativos
    {
        [SetUp]
        public void SetUp()
        {
            _factory = new StorageFactory();
        }

        private StorageFactory _factory = null!;

        [TestCase("archivo.txt", typeof(CitaJsonStorage))]
        [TestCase("documento.yaml", typeof(CitaJsonStorage))]
        [TestCase("imagen.png", typeof(CitaJsonStorage))]
        public void Crear_ConExtensionNoSoportada_DeberiaHacerFallbackAJsonStorage(string path, Type tipoEsperado)
        {
            // Act
            var resultado = _factory.Crear(path);

            // Assert
            resultado.Should().NotBeNull();
            resultado.Should().BeOfType(tipoEsperado);
        }
    }

    [TestFixture]
    public class CasosMixtos
    {
        [SetUp]
        public void SetUp()
        {
            _factory = new StorageFactory();
            
            var configurationField = typeof(AppConfig).GetField("Configuration", BindingFlags.Static | BindingFlags.NonPublic);
            _config = (IConfiguration)configurationField?.GetValue(null)!;
            
            _originalStorageType = _config["Storage:Type"];
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null)
            {
                _config["Storage:Type"] = _originalStorageType;
            }
        }

        private StorageFactory _factory = null!;
        private IConfiguration _config = null!;
        private string? _originalStorageType;

        [TestCase("archivoSinExtension", "xml", typeof(CitaXmlStorage))]
        [TestCase("archivo.", "csv", typeof(CitaCsvStorage))]
        [TestCase("ruta/vacia/", "json", typeof(CitaJsonStorage))]
        [TestCase(null, "xml", typeof(CitaXmlStorage))]
        [TestCase(null, "yaml", typeof(CitaJsonStorage))]
        public void Crear_CuandoLaRutaNoTieneExtension_DeberiaUtilizarConfiguracionGlobalDeAppConfig(string? path, string configStorageType, Type tipoEsperado)
        {
            // Arrange
            _config["Storage:Type"] = configStorageType;

            // Act
            var resultado = _factory.Crear(path!);

            // Assert
            resultado.Should().NotBeNull();
            resultado.Should().BeOfType(tipoEsperado);
        }
    }
}