using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CSharpFunctionalExtensions;
using ITVLuisVives.Back.Entity;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Mappers;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Factories.Vehiculos;
using Serilog;

namespace ITVLuisVives.Back.Repositories;

/// <summary>
///     Repositorio de vehículos que utiliza ADO.NET con SQLite.
/// </summary>
public class VehiculoAdoRepository : IVehiculoRepository
{
    private readonly IDbConnection _connection;
    private readonly Action? _onDispose;
    private readonly ILogger _logger = Log.ForContext<VehiculoAdoRepository>();

    public VehiculoAdoRepository(IDbConnection connection, Action? onDispose = null, bool dropData = false, bool seedData = false)
    {
        _connection = connection;
        _onDispose = onDispose;
        EnsureTable(dropData);

        if (seedData && CountTotal() == 0)
        {
            _logger.Information("Sembrando datos iniciales de vehículos en ADO.NET");
            Seed();
        }
    }

    public IEnumerable<Vehiculo> GetFiltered(
        string? matricula, 
        string? marca, 
        TipoMotor? motor, 
        DateTime? matriculacionDesde, 
        DateTime? matriculacionHasta, 
        int page = 1, 
        int pageSize = 10, 
        bool includeDeleted = false)
    {
        var list = new List<VehiculoEntity>();
        try
        {
            EnsureConnectionOpen();
            using var command = _connection.CreateCommand();
            var sql = "SELECT * FROM Vehiculos WHERE 1=1";

            if (!includeDeleted) sql += " AND IsDeleted = 0";
            
            if (!string.IsNullOrWhiteSpace(matricula)) 
            { 
                sql += " AND LOWER(Matricula) LIKE @Matricula"; 
                AddParameter(command, "@Matricula", $"%{matricula.ToLower()}%"); 
            }
            if (!string.IsNullOrWhiteSpace(marca)) 
            { 
                sql += " AND LOWER(Marca) LIKE @Marca"; 
                AddParameter(command, "@Marca", $"%{marca.ToLower()}%"); 
            }
            if (motor.HasValue) 
            { 
                sql += " AND Motor = @Motor"; 
                AddParameter(command, "@Motor", motor.Value.ToString()); 
            }
            if (matriculacionDesde.HasValue) 
            { 
                sql += " AND FechaMatriculacion >= @Desde"; 
                AddParameter(command, "@Desde", matriculacionDesde.Value.ToString("o")); 
            }
            if (matriculacionHasta.HasValue) 
            { 
                sql += " AND FechaMatriculacion <= @Hasta"; 
                AddParameter(command, "@Hasta", matriculacionHasta.Value.ToString("o")); 
            }

            sql += " ORDER BY Id LIMIT @PageSize OFFSET @Offset";
            AddParameter(command, "@PageSize", pageSize);
            AddParameter(command, "@Offset", (page - 1) * pageSize);

            command.CommandText = sql;
            using var reader = command.ExecuteReader();
            while (reader.Read()) list.Add(MapReaderToEntity(reader));

            return list.Select(v => v.ToModel()).OfType<Vehiculo>().ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al filtrar vehículos en ADO.NET");
            return [];
        }
    }

    public Vehiculo? GetById(int id)
    {
        try
        {
            EnsureConnectionOpen();
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT * FROM Vehiculos WHERE Id = @Id";
            AddParameter(command, "@Id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read()) return MapReaderToEntity(reader).ToModel();
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al buscar vehículo por ID {Id} en ADO.NET", id);
            return null;
        }
    }

    public Vehiculo? GetByMatricula(string matricula)
    {
        try
        {
            EnsureConnectionOpen();
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT * FROM Vehiculos WHERE LOWER(Matricula) = LOWER(@Matricula)";
            AddParameter(command, "@Matricula", matricula);

            using var reader = command.ExecuteReader();
            if (reader.Read()) return MapReaderToEntity(reader).ToModel();
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al buscar vehículo por Matrícula {Matricula} en ADO.NET", matricula);
            return null;
        }
    }

    public bool ExisteMatricula(string matricula)
    {
        try
        {
            EnsureConnectionOpen();
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM Vehiculos WHERE LOWER(Matricula) = LOWER(@Matricula) AND IsDeleted = 0";
            AddParameter(command, "@Matricula", matricula);

            var count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al verificar existencia de matrícula {Matricula} en ADO.NET", matricula);
            return false;
        }
    }

    public Result<Vehiculo, DomainError> Create(Vehiculo model)
    {
        model = model with { CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now, IsDeleted = false, DeletedAt = null };
        var entity = model.ToEntity();

        try
        {
            EnsureConnectionOpen();
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Vehiculos (Matricula, Marca, Modelo, Motor, FechaMatriculacion, CreatedAt, UpdatedAt, IsDeleted, DeletedAt)
                VALUES (@Matricula, @Marca, @Modelo, @Motor, @FechaMatriculacion, @CreatedAt, @UpdatedAt, @IsDeleted, @DeletedAt);
                SELECT last_insert_rowid();"; 

            AddParameter(command, "@Matricula", entity!.Matricula);
            AddParameter(command, "@Marca", entity.Marca);
            AddParameter(command, "@Modelo", entity.Modelo);
            AddParameter(command, "@Motor", entity.Motor);
            AddParameter(command, "@FechaMatriculacion", entity.FechaMatriculacion.ToString("o"));
            AddParameter(command, "@CreatedAt", entity.CreatedAt.ToString("o"));
            AddParameter(command, "@UpdatedAt", entity.UpdatedAt.ToString("o"));
            AddParameter(command, "@IsDeleted", entity.IsDeleted ? 1 : 0);
            AddParameter(command, "@DeletedAt", entity.DeletedAt?.ToString("o") ?? (object)DBNull.Value);

            var generatedId = Convert.ToInt32(command.ExecuteScalar());
            return Result.Success<Vehiculo, DomainError>(GetById(generatedId)!);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al crear vehículo en ADO.NET");
            return Result.Failure<Vehiculo, DomainError>(new DomainError(ex.Message, "DATABASE_ERROR"));
        }
    }

    public Result<Vehiculo, DomainError> Update(int id, Vehiculo model)
    {
        try
        {
            var existing = GetById(id);
            if (existing == null) return Result.Failure<Vehiculo, DomainError>(DomainError.NotFound("Vehiculo", id.ToString()));

            model = model with { Id = id, CreatedAt = existing.CreatedAt, UpdatedAt = DateTime.Now };
            var entity = model.ToEntity();

            EnsureConnectionOpen();
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                UPDATE Vehiculos SET 
                    Matricula = @Matricula, Marca = @Marca, Modelo = @Modelo, 
                    Motor = @Motor, FechaMatriculacion = @FechaMatriculacion, UpdatedAt = @UpdatedAt
                WHERE Id = @Id";

            AddParameter(command, "@Id", id);
            AddParameter(command, "@Matricula", entity!.Matricula);
            AddParameter(command, "@Marca", entity.Marca);
            AddParameter(command, "@Modelo", entity.Modelo);
            AddParameter(command, "@Motor", entity.Motor);
            AddParameter(command, "@FechaMatriculacion", entity.FechaMatriculacion.ToString("o"));
            AddParameter(command, "@UpdatedAt", entity.UpdatedAt.ToString("o"));

            command.ExecuteNonQuery();
            return Result.Success<Vehiculo, DomainError>(GetById(id)!);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al actualizar vehículo con ID {Id} en ADO.NET", id);
            return Result.Failure<Vehiculo, DomainError>(new DomainError(ex.Message, "DATABASE_ERROR"));
        }
    }

    public Vehiculo? Delete(int id, bool isLogical = true)
    {
        try
        {
            var existing = GetById(id);
            if (existing == null) return null;

            EnsureConnectionOpen();
            using var command = _connection.CreateCommand();

            if (isLogical)
            {
                command.CommandText = "UPDATE Vehiculos SET IsDeleted = 1, DeletedAt = @DeletedAt, UpdatedAt = @UpdatedAt WHERE Id = @Id";
                AddParameter(command, "@Id", id);
                AddParameter(command, "@DeletedAt", DateTime.Now.ToString("o"));
                AddParameter(command, "@UpdatedAt", DateTime.Now.ToString("o"));
                command.ExecuteNonQuery();
                return GetById(id);
            }
            else
            {
                command.CommandText = "DELETE FROM Vehiculos WHERE Id = @Id";
                AddParameter(command, "@Id", id);
                command.ExecuteNonQuery();
                return existing;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al eliminar vehículo con ID {Id} en ADO.NET", id);
            return null;
        }
    }

    public Result<Vehiculo, DomainError> Restore(int id)
    {
        try
        {
            var existing = GetById(id);
            if (existing == null) return Result.Failure<Vehiculo, DomainError>(DomainError.NotFound("Vehiculo", id.ToString()));

            EnsureConnectionOpen();
            using var command = _connection.CreateCommand();
            command.CommandText = "UPDATE Vehiculos SET IsDeleted = 0, DeletedAt = NULL, UpdatedAt = @UpdatedAt WHERE Id = @Id";
            AddParameter(command, "@Id", id);
            AddParameter(command, "@UpdatedAt", DateTime.Now.ToString("o"));

            command.ExecuteNonQuery();
            return Result.Success<Vehiculo, DomainError>(GetById(id)!);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al restaurar vehículo con ID {Id} en ADO.NET", id);
            return Result.Failure<Vehiculo, DomainError>(new DomainError(ex.Message, "DATABASE_ERROR"));
        }
    }

    public int Count(bool includeDeleted = false)
    {
        EnsureConnectionOpen();
        using var command = _connection.CreateCommand();
        command.CommandText = includeDeleted ? "SELECT COUNT(1) FROM Vehiculos" : "SELECT COUNT(1) FROM Vehiculos WHERE IsDeleted = 0";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private int CountTotal()
    {
        EnsureConnectionOpen();
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM Vehiculos";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private void EnsureConnectionOpen() { if (_connection.State != ConnectionState.Open) _connection.Open(); }

    private void AddParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private VehiculoEntity MapReaderToEntity(IDataReader reader)
    {
        return new VehiculoEntity {
            Id = Convert.ToInt32(reader["Id"]),
            Matricula = reader["Matricula"].ToString()!,
            Marca = reader["Marca"].ToString()!,
            Modelo = reader["Modelo"].ToString()!,
            Motor = reader["Motor"].ToString()!,
            FechaMatriculacion = DateTime.Parse(reader["FechaMatriculacion"].ToString()!),
            CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString()!),
            UpdatedAt = DateTime.Parse(reader["UpdatedAt"].ToString()!),
            IsDeleted = Convert.ToInt32(reader["IsDeleted"]) == 1,
            DeletedAt = reader["DeletedAt"] != DBNull.Value ? DateTime.Parse(reader["DeletedAt"].ToString()!) : null
        };
    }

    private void EnsureTable(bool dropData)
    {
        EnsureConnectionOpen();
        using var command = _connection.CreateCommand();
        if (dropData) { command.CommandText = "DROP TABLE IF EXISTS Vehiculos"; command.ExecuteNonQuery(); }

        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Vehiculos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Matricula TEXT NOT NULL UNIQUE,
                Marca TEXT NOT NULL,
                Modelo TEXT NOT NULL,
                Motor TEXT NOT NULL,
                FechaMatriculacion TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                DeletedAt TEXT
            );";
        command.ExecuteNonQuery();
    }

    private void Seed()
    {
        foreach (var v in VehiculoFactory.Seed()) Create(v);
    }
}