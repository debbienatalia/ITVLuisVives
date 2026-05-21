using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CSharpFunctionalExtensions;
using Dapper;
using ITVLuisVives.Back.Entity;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Mappers;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Factories.Vehiculos;
using Serilog;

namespace ITVLuisVives.Back.Repositories;

/// <summary>
///     Repositorio de vehículos que utiliza Dapper con SQLite.
/// </summary>
public class VehiculoDapperRepository : IVehiculoRepository
{
    private readonly IDbConnection _connection;
    private readonly Action? _onDispose;
    private readonly ILogger _logger = Log.ForContext<VehiculoDapperRepository>();

    public VehiculoDapperRepository(IDbConnection connection, Action? onDispose = null, bool dropData = false, bool seedData = false)
    {
        _connection = connection;
        _onDispose = onDispose;
        
        EnsureTable(dropData);

        if (seedData && CountTotal() == 0)
        {
            Seed();
        }
    }

    public IEnumerable<Vehiculo> GetFiltered(string? matricula, string? marca, TipoMotor? motor, DateTime? matriculacionDesde, DateTime? matriculacionHasta, int page = 1, int pageSize = 10, bool includeDeleted = false)
    {
        try
        {
            var sql = "SELECT * FROM Vehiculos WHERE 1=1";
            var parameters = new DynamicParameters();

            if (!includeDeleted)
            {
                sql += " AND IsDeleted = 0";
            }

            if (!string.IsNullOrWhiteSpace(matricula))
            {
                sql += " AND LOWER(Matricula) LIKE @Matricula";
                parameters.Add("Matricula", $"%{matricula.ToLower()}%");
            }

            if (!string.IsNullOrWhiteSpace(marca))
            {
                sql += " AND LOWER(Marca) LIKE @Marca";
                parameters.Add("Marca", $"%{marca.ToLower()}%");
            }

            if (motor.HasValue)
            {
                sql += " AND Motor = @Motor";
                parameters.Add("Motor", motor.Value.ToString());
            }

            if (matriculacionDesde.HasValue)
            {
                sql += " AND FechaMatriculacion >= @Desde";
                parameters.Add("Desde", matriculacionDesde.Value.ToString("o"));
            }

            if (matriculacionHasta.HasValue)
            {
                sql += " AND FechaMatriculacion <= @Hasta";
                parameters.Add("Hasta", matriculacionHasta.Value.ToString("o"));
            }

            sql += " ORDER BY Id LIMIT @PageSize OFFSET @Offset";
            parameters.Add("PageSize", pageSize);
            parameters.Add("Offset", (page - 1) * pageSize);

            var entities = _connection.Query<VehiculoEntity>(sql, parameters).ToList();
            return entities.Select(v => v.ToModel()).OfType<Vehiculo>().ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al obtener vehículos filtrados");
            return [];
        }
    }

    public Vehiculo? GetById(int id)
    {
        try
        {
            const string sql = "SELECT * FROM Vehiculos WHERE Id = @Id";
            var entity = _connection.QueryFirstOrDefault<VehiculoEntity>(sql, new { Id = id });
            return entity?.ToModel();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al obtener vehículo por ID {Id}", id);
            return null;
        }
    }

    public Vehiculo? GetByMatricula(string matricula)
    {
        try
        {
            const string sql = "SELECT * FROM Vehiculos WHERE LOWER(Matricula) = LOWER(@Matricula)";
            var entity = _connection.QueryFirstOrDefault<VehiculoEntity>(sql, new { Matricula = matricula });
            return entity?.ToModel();
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error al obtener vehículo por Matrícula {Matricula}", matricula);
            return null;
        }
    }

    public bool ExisteMatricula(string matricula)
    {
        try
        {
            const string sql = "SELECT COUNT(1) FROM Vehiculos WHERE LOWER(Matricula) = LOWER(@Matricula) AND IsDeleted = 0";
            return _connection.ExecuteScalar<int>(sql, new { Matricula = matricula }) > 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al verificar existencia de matrícula {Matricula}", matricula);
            return false;
        }
    }

    public Result<Vehiculo, DomainError> Create(Vehiculo model)
    {
        if (ExisteMatricula(model.Matricula))
            return Result.Failure<Vehiculo, DomainError>(new DomainError($"La matrícula '{model.Matricula}' ya está registrada.", "MATRICULA_DUPLICADA"));

        model = model with
        {
            Id = 0,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsDeleted = false,
            DeletedAt = null
        };

        var entity = model.ToEntity();

        try
        {
            const string sql = @"
                INSERT INTO Vehiculos (Matricula, Marca, Modelo, Motor, FechaMatriculacion, CreatedAt, UpdatedAt, IsDeleted, DeletedAt)
                VALUES (@Matricula, @Marca, @Modelo, @Motor, @FechaMatriculacion, @CreatedAt, @UpdatedAt, @IsDeleted, @DeletedAt);
                SELECT last_insert_rowid();";

            entity.Id = _connection.ExecuteScalar<int>(sql, new {
                entity.Matricula,
                entity.Marca,
                entity.Modelo,
                entity.Motor,
                FechaMatriculacion = entity.FechaMatriculacion.ToString("o"),
                CreatedAt = entity.CreatedAt.ToString("o"),
                UpdatedAt = entity.UpdatedAt.ToString("o"),
                IsDeleted = entity.IsDeleted ? 1 : 0,
                DeletedAt = entity.DeletedAt?.ToString("o")
            });

            return Result.Success<Vehiculo, DomainError>(GetById(entity.Id)!);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al crear vehículo");
            return Result.Failure<Vehiculo, DomainError>(new DomainError(ex.Message, "DATABASE_ERROR"));
        }
    }

    public Result<Vehiculo, DomainError> Update(int id, Vehiculo model)
    {
        try
        {
            var existing = GetById(id);
            if (existing == null)
                return Result.Failure<Vehiculo, DomainError>(DomainError.NotFound("Vehiculo", id.ToString()));

            if (model.Matricula.ToLower() != existing.Matricula.ToLower() && ExisteMatricula(model.Matricula))
                return Result.Failure<Vehiculo, DomainError>(new DomainError($"La matrícula '{model.Matricula}' ya pertenece a otro vehículo.", "MATRICULA_DUPLICADA"));

            model = model with
            {
                Id = id,
                CreatedAt = existing.CreatedAt,
                UpdatedAt = DateTime.Now
            };

            var entity = model.ToEntity();

            const string sql = @"
                UPDATE Vehiculos SET 
                    Matricula = @Matricula, Marca = @Marca, Modelo = @Modelo, Motor = @Motor, 
                    FechaMatriculacion = @FechaMatriculacion, UpdatedAt = @UpdatedAt
                WHERE Id = @Id";

            _connection.Execute(sql, new {
                Id = id,
                entity.Matricula,
                entity.Marca,
                entity.Modelo,
                entity.Motor,
                FechaMatriculacion = entity.FechaMatriculacion.ToString("o"),
                UpdatedAt = entity.UpdatedAt.ToString("o")
            });

            return Result.Success<Vehiculo, DomainError>(GetById(id)!);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al actualizar vehículo con ID {Id}", id);
            return Result.Failure<Vehiculo, DomainError>(new DomainError(ex.Message, "DATABASE_ERROR"));
        }
    }

    public Vehiculo? Delete(int id, bool isLogical = true)
    {
        try
        {
            var existing = GetById(id);
            if (existing == null) return null;

            if (isLogical)
            {
                const string sql = "UPDATE Vehiculos SET IsDeleted = 1, DeletedAt = @DeletedAt, UpdatedAt = @UpdatedAt WHERE Id = @Id";
                _connection.Execute(sql, new { Id = id, DeletedAt = DateTime.Now.ToString("o"), UpdatedAt = DateTime.Now.ToString("o") });
                return GetById(id);
            }
            else
            {
                const string sql = "DELETE FROM Vehiculos WHERE Id = @Id";
                _connection.Execute(sql, new { Id = id });
                return existing;
            }
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error al eliminar vehículo con ID {Id}", id);
            return null;
        }
    }

    public Result<Vehiculo, DomainError> Restore(int id)
    {
        try
        {
            var existing = GetById(id);
            if (existing == null)
                return Result.Failure<Vehiculo, DomainError>(DomainError.NotFound("Vehiculo", id.ToString()));

            const string sql = "UPDATE Vehiculos SET IsDeleted = 0, DeletedAt = NULL, UpdatedAt = @UpdatedAt WHERE Id = @Id";
            _connection.Execute(sql, new { Id = id, UpdatedAt = DateTime.Now.ToString("o") });

            _logger.Information("Vehículo con ID {Id} restaurado correctamente", id);
            return Result.Success<Vehiculo, DomainError>(GetById(id)!);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al restaurar vehículo con ID {Id}", id);
            return Result.Failure<Vehiculo, DomainError>(new DomainError(ex.Message, "DATABASE_ERROR"));
        }
    }

    public int Count(bool includeDeleted = false)
    {
        try
        {
            var sql = includeDeleted ? "SELECT COUNT(1) FROM Vehiculos" : "SELECT COUNT(1) FROM Vehiculos WHERE IsDeleted = 0";
            return _connection.ExecuteScalar<int>(sql);
        }
        catch
        {
            return 0;
        }
    }

    private int CountTotal()
    {
        return _connection.ExecuteScalar<int>("SELECT COUNT(1) FROM Vehiculos");
    }

    private void EnsureTable(bool dropData)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        if (dropData) 
            _connection.Execute("DROP TABLE IF EXISTS Vehiculos");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Vehiculos (
                Id INTEGER PRIMARY KEY,
                Matricula TEXT NOT NULL UNIQUE,
                Marca TEXT NOT NULL,
                Modelo TEXT NOT NULL,
                Motor TEXT NOT NULL,
                FechaMatriculacion TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                DeletedAt TEXT
            )");
    }

    private void Seed()
    {
        foreach (var v in VehiculoFactory.Seed()) 
            Create(v);
    }
}