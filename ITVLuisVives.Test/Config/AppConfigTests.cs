using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using ITVLuisVives.Back.Config;
using Microsoft.Extensions.Configuration;

namespace ITVLuisVives.Test.Config;

[TestFixture]
public class AppConfigTests {

    [TestFixture]
    public class StorageYRepository {
        [Test]
        public void StorageType_DeberiaRetornarTipoConfigurado() {
            // Act
            var tipo = AppConfig.StorageType;

            // Assert
            tipo.Should().NotBeNullOrEmpty();
            tipo.Should().Be("Json");
        }

        [Test]
        public void RepositoryType_DeberiaRetornarTipoConfigurado() {
            // Act
            var tipo = AppConfig.RepositoryType;

            // Assert
            tipo.Should().NotBeNullOrEmpty();
            tipo.Should().Be("EFCore");
        }

        [Test]
        public void ConnectionString_DeberiaRetornarCadenaValida() {
            // Act
            var connStr = AppConfig.ConnectionString;

            // Assert
            connStr.Should().NotBeNullOrEmpty();
            connStr.Should().Contain("Data Source");
        }

        [Test]
        public void FlagsDePersistencia_DeberianRetornarValoresConfigurados() {
            // Act
            var drop = AppConfig.DropData;
            var seed = AppConfig.SeedData;
            var logical = AppConfig.UseLogicalDelete;

            // Assert
            drop.Should().BeTrue();
            seed.Should().BeTrue();
            logical.Should().BeTrue();
        }
    }

    [TestFixture]
    public class DirectoriosYCache {
        [Test]
        public void RepositoryDirectory_DeberiaRetornarRutaConfigurada() {
            // Act
            var dir = AppConfig.RepositoryDirectory;

            // Assert
            dir.Should().NotBeNullOrEmpty();
            dir.Should().Be("data");
        }

        [Test]
        public void BackupYReportsDirectory_DeberianRetornarRutasValidas() {
            // Act
            var backupDir = AppConfig.BackupDirectory;
            var reportsDir = AppConfig.ReportsDirectory;

            // Assert
            backupDir.Should().Be("backup");
            reportsDir.Should().Be("reports");
        }

        [Test]
        public void CacheSize_DeberiaSerMayorQueCero() {
            // Act
            var size = AppConfig.CacheSize;

            // Assert
            size.Should().Be(5);
        }
    }

    [TestFixture]
    public class ReglasNegocioYFormatos {
        [Test]
        public void MaxCitasPorDniYDia_DeberiaRetornarLimiteValido() {
            // Act
            var maxCitas = AppConfig.MaxCitasPorDniYDia;

            // Assert
            maxCitas.Should().BeGreaterThan(0);
            maxCitas.Should().Be(3);
        }

        [Test]
        public void PlazoMaximoDiasCita_DeberiaRetornarRangoValido() {
            // Act
            var plazo = AppConfig.PlazoMaximoDiasCita;

            // Assert
            plazo.Should().BeGreaterThan(0);
            plazo.Should().Be(30);
        }

        [Test]
        public void BackupFormat_DeberiaRetornarFormatoConfigurado() {
            // Act
            var format = AppConfig.BackupFormat;

            // Assert
            format.Should().NotBeNullOrEmpty();
            format.Should().Be("Json");
        }

        [Test]
        public void IsDevelopmentEnabled_DeberiaRetornarTrue() {
            // Act
            var dev = AppConfig.IsDevelopmentEnabled;

            // Assert
            dev.Should().BeTrue();
        }
    }

    [TestFixture]
    public class CoberturaRamasOpcionales {
        [Test]
        public void CuandoElArchivoEstaVacio_DeberianActivarseLosValoresPorDefecto() {
            // Arrange
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!File.Exists(path)) return;

            var originalContent = File.ReadAllText(path);

            try {
                // Act
                File.WriteAllText(path, "{}");
                ForzarRecargaConfiguracion();

                // Assert
                AppConfig.StorageType.Should().Be("Json");
                AppConfig.RepositoryType.Should().Be("EFCore");
                AppConfig.RepositoryDirectory.Should().Be("data");
                AppConfig.ConnectionString.Should().Be("Data Source=data/itv_luisvives.db");
                AppConfig.BackupDirectory.Should().Be("backup");
                AppConfig.BackupFormat.Should().Be("Json");
                AppConfig.ReportsDirectory.Should().Be("reports");
                
                AppConfig.DropData.Should().BeFalse();
                AppConfig.SeedData.Should().BeTrue();
                AppConfig.UseLogicalDelete.Should().BeTrue();
                AppConfig.CacheSize.Should().Be(5);
                AppConfig.MaxCitasPorDniYDia.Should().Be(3);
                AppConfig.PlazoMaximoDiasCita.Should().Be(30);
                AppConfig.IsDevelopmentEnabled.Should().BeFalse();
            }
            finally {
                File.WriteAllText(path, originalContent);
                ForzarRecargaConfiguracion();
            }
        }

        private static void ForzarRecargaConfiguracion() {
            var campos = typeof(AppConfig).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            var campoConfig = campos.FirstOrDefault(f => typeof(IConfiguration).IsAssignableFrom(f.FieldType));

            if (campoConfig?.GetValue(null) is IConfigurationRoot configRoot) {
                configRoot.Reload();
            }
        }
    }
}