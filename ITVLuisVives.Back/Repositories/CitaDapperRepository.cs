using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CSharpFunctionalExtensions;
using Dapper;
using ITVLuisVives.Back.Entity;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Mappers;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Factories;
using Serilog;

namespace ITVLuisVives.Back.Repositories;

/// <summary>
///     Repositorio de citas que utiliza Dapper con SQLite.
/// </summary>
public class CitaDapperRepository : ICitaRepository
{
    private readonly IDbConnection _connection;
    private readonly Action? _onDispose;
    private readonly ILogger _logger = Log.ForContext<CitaDapperRepository>();

    public CitaDapperRepository(IDbConnection connection, Action? onDispose = null, bool dropData = false, bool seedData = false)
    {
        _connection = connection;
        _onDispose = onDispose;
        
        EnsureTable(dropData);

        if (seedData && CountTotal() == 0)
        {
            _logger.Information("Sembrando datos iniciales de citas en Dapper...");
            Seed();
        }
    }

    public IEnumerable<Cita> GetFiltered(string? dni, string? matricula, EstadoCita? estado, DateTime? fechaDesde, DateTime? fechaHasta, int page = 1, int pageSize = 10, bool includeDeleted = false)
    {
        try
        {
            var sql = "SELECT * FROM Citas WHERE 1=1";
            var parameters = new DynamicParameters();

            if (!includeDeleted)
            {
                sql += " AND IsDeleted = 0";
            }

            if (!string.IsNullOrWhiteSpace(dni))
            {
                sql += " AND LOWER(Dni) LIKE @Dni";
                parameters.Add("Dni", $"%{dni.ToLower()}%");
            }

            if (!string.IsNullOrWhiteSpace(matricula))
            {
                sql += " AND LOWER(VehiculoMatricula) LIKE @Matricula";
                parameters.Add("Matricula", $"%{matricula.ToLower()}%");
            }

            if (estado.HasValue)
            {
                sql += " AND Estado = @Estado";
                parameters.Add("Estado", estado.Value.ToString());
            }

            if (fechaDesde.HasValue)
            {
                sql += " AND FechaInspeccion >= @Desde";
                parameters.Add("Desde", fechaDesde.Value.ToString("o"));
            }

            if (fechaHasta.HasValue)
            {
                sql += " AND FechaInspeccion <= @Hasta";
                parameters.Add("Hasta", fechaHasta.Value.ToString("o"));
            }

            sql += " ORDER BY FechaInspeccion LIMIT @PageSize OFFSET @Offset";
            parameters.Add("PageSize", pageSize);
            parameters.Add("Offset", (page - 1) * pageSize);

            var entities = _connection.Query<CitaEntity>(sql, parameters).ToList();
            return entities.Select(c => c.ToModel()).OfType<Cita>().ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al obtener citas filtradas en Dapper");
            return [];
        }
    }

    public Cita? GetById(Guid id)
    {
        try
        {
            const string sql = "SELECT * FROM Citas WHERE Id = @Id";
            var entity = _connection.QueryFirstOrDefault<CitaEntity>(sql, new { Id = id.ToString() });
            return entity?.ToModel();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al obtener cita por ID {Id} en Dapper", id);
            return null;
        }
    }

    public bool ExisteCitaParaVehiculoEnFecha(string matricula, DateTime fecha)
    {
        try
        {
            // Usamos la función date() de SQLite para comparar únicamente la parte de la fecha (AAAA-MM-DD)
            const string sql = "SELECT COUNT(1) FROM Citas WHERE IsDeleted = 0 AND LOWER(VehiculoMatricula) = LOWER(@Matricula) AND date(FechaInspeccion) = date(@Fecha)";
            return _connection.ExecuteScalar<int>(sql, new { Matricula = matricula, Fecha = fecha.ToString("o") }) > 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al comprobar cita del vehículo {Matricula} en fecha {Fecha} con Dapper", matricula, fecha);
            return false;
        }
    }

    public int CountCitasPorDniYFecha(string dni, DateTime fecha)
    {
        try
        {
            const string sql = "SELECT COUNT(1) FROM Citas WHERE IsDeleted = 0 AND LOWER(Dni) = LOWER(@Dni) AND date(FechaInspeccion) = date(@Fecha)";
            return _connection.ExecuteScalar<int>(sql, new { Dni = dni, Fecha = fecha.ToString("o") });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al contar citas del DNI {Dni} en fecha {Fecha} con Dapper", dni, fecha);
            return 0;
        }
    }

    public Result<Cita, DomainError> Create(Cita model)
    {
        model = model with
        {
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsDeleted = false,
            DeletedAt = null
        };

        var entity = model.ToEntity();
        if (entity == null) return Result.Failure<Cita, DomainError>(new DomainError("Error en el mapeo", "MAPPER_ERROR"));

        try
        {
            const string sql = @"
                INSERT INTO Citas (Id, Dni, VehiculoMatricula, FechaInspeccion, Estado, Observaciones, CreatedAt, UpdatedAt, IsDeleted, DeletedAt)
                VALUES (@Id, @Dni, @VehiculoMatricula, @FechaInspeccion, @Estado, @Observaciones, @CreatedAt, @UpdatedAt, @IsDeleted, @DeletedAt);";

            _connection.Execute(sql, new {
                Id = entity.Id.ToString(),
                entity.Dni,
                entity.VehiculoMatricula,
                FechaInspeccion = entity.FechaInspeccion.ToString("o"),
                entity.Estado,
                entity.Observaciones,
                CreatedAt = entity.CreatedAt.ToString("o"),
                UpdatedAt = entity.UpdatedAt.ToString("o"),
                IsDeleted = entity.IsDeleted ? 1 : 0,
                DeletedAt = entity.DeletedAt?.ToString("o")
            });

            return Result.Success<Cita, DomainError>(GetById(entity.Id)!);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al crear cita en Dapper");
            return Result.Failure<Cita, DomainError>(new DomainError(ex.Message, "DATABASE_ERROR"));
        }
    }

    public Result<Cita, DomainError> Update(Guid id, Cita model)
    {
        try
        {
            var existing = GetById(id);
            if (existing == null)
                return Result.Failure<Cita, DomainError>(DomainError.NotFound("Cita", id.ToString()));

            model = model with
            {
                Id = id,
                CreatedAt = existing.CreatedAt,
                UpdatedAt = DateTime.Now
            };

            var entity = model.ToEntity();

            const string sql = @"
                UPDATE Citas SET 
                    Dni = @Dni, VehiculoMatricula = @VehiculoMatricula, FechaInspeccion = @FechaInspeccion, 
                    Estado = @Estado, Observaciones = @Observaciones, UpdatedAt = @UpdatedAt
                WHERE Id = @Id";

            _connection.Execute(sql, new {
                Id = id.ToString(),
                entity.Dni,
                entity.VehiculoMatricula,
                FechaInspeccion = entity.FechaInspeccion.ToString("o"),
                entity.Estado,
                entity.Observaciones,
                UpdatedAt = entity.UpdatedAt.ToString("o")
            });

            return Result.Success<Cita, DomainError>(GetById(id)!);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al actualizar cita con ID {Id} en Dapper", id);
            return Result.Failure<Cita, DomainError>(new DomainError(ex.Message, "DATABASE_ERROR"));
        }
    }

    public Cita? Delete(Guid id, bool isLogical = true)
    {
        try
        {
            var existing = GetById(id);
            if (existing == null) return null;

            if (isLogical)
            {
                const string sql = "UPDATE Citas SET IsDeleted = 1, DeletedAt = @DeletedAt, UpdatedAt = @UpdatedAt WHERE Id = @Id";
                _connection.Execute(sql, new { Id = id.ToString(), DeletedAt = DateTime.Now.ToString("o"), UpdatedAt = DateTime.Now.ToString("o") });
                return GetById(id);
            }
            else
            {
                const string sql = "DELETE FROM Citas WHERE Id = @Id";
                _connection.Execute(sql, new { Id = id.ToString() });
                return existing;
            }
        }
        catch (Exception ex) 
        {
            _logger.Error(ex, "Error al eliminar cita con ID {Id} en Dapper", id);
            return null;
        }
    }

    public Result<Cita, DomainError> Restore(Guid id)
    {
        try
        {
            var existing = GetById(id);
            if (existing == null)
                return Result.Failure<Cita, DomainError>(DomainError.NotFound("Cita", id.ToString()));

            const string sql = "UPDATE Citas SET IsDeleted = 0, DeletedAt = NULL, UpdatedAt = @UpdatedAt WHERE Id = @Id";
            _connection.Execute(sql, new { Id = id.ToString(), UpdatedAt = DateTime.Now.ToString("o") });

            return Result.Success<Cita, DomainError>(GetById(id)!);
        }
        catch (Exception ex) 
        {
            _logger.Error(ex, "Error al restaurar cita con ID {Id} en Dapper", id);
            return Result.Failure<Cita, DomainError>(new DomainError(ex.Message, "DATABASE_ERROR"));
        }
    }

    public int Count(bool includeDeleted = false)
    {
        try
        {
            var sql = includeDeleted ? "SELECT COUNT(1) FROM Citas" : "SELECT COUNT(1) FROM Citas WHERE IsDeleted = 0";
            return _connection.ExecuteScalar<int>(sql);
        }
        catch
        {
            return 0;
        }
    }

    private int CountTotal()
    {
        return _connection.ExecuteScalar<int>("SELECT COUNT(1) FROM Citas");
    }

    private void EnsureTable(bool dropData)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        if (dropData) 
            _connection.Execute("DROP TABLE IF EXISTS Citas");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Citas (
                Id TEXT PRIMARY KEY,
                Dni TEXT NOT NULL,
                VehiculoMatricula TEXT NOT NULL,
                FechaInspeccion TEXT NOT NULL,
                Estado TEXT NOT NULL,
                Observaciones TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                DeletedAt TEXT
            )");
    }

    private void Seed()
    {
        foreach (var c in CitaFactory.Seed()) 
            Create(c);
    }
}