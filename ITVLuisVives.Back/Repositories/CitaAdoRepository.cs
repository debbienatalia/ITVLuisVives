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
using ITVLuisVives.Back.Factories.Citas;
using Serilog;

namespace ITVLuisVives.Back.Repositories.Citas;

/// <summary>
///     Repositorio de citas que utiliza ADO.NET con SQLite.
/// </summary>
public class CitaAdoRepository : ICitaRepository
{
    private readonly IDbConnection _connection;
    private readonly Action? _onDispose;
    private readonly ILogger _logger = Log.ForContext<CitaAdoRepository>();

    public CitaAdoRepository(IDbConnection connection, Action? onDispose = null, bool dropData = false, bool seedData = false)
    {
        _connection = connection;
        _onDispose = onDispose;
        EnsureTable(dropData);

        if (seedData && CountTotal() == 0)
        {
            _logger.Information("Sembrando datos iniciales de citas en ADO.NET");
            Seed();
        }
    }

    public IEnumerable<Cita> GetFiltered(
        string? dni,
        string? matricula,
        EstadoCita? estado,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int page = 1,
        int pageSize = 10,
        bool includeDeleted = false)
    {
        var list = new List<CitaEntity>();
        try
        {
            EnsureConnectionOpen();
            using var command = _connection.CreateCommand();
            var sql = "SELECT * FROM Citas WHERE 1=1";

            if (!includeDeleted) sql += " AND IsDeleted = 0";
            
            if (!string.IsNullOrWhiteSpace(dni))
            {
                sql += " AND LOWER(Dni) LIKE @Dni";
                AddParameter(command, "@Dni", $"%{dni.ToLower()}%");
            }
            if (!string.IsNullOrWhiteSpace(matricula))
            {
                sql += " AND LOWER(VehiculoMatricula) LIKE @Matricula";
                AddParameter(command, "@Matricula", $"%{matricula.ToLower()}%");
            }
            if (estado.HasValue)
            {
                sql += " AND Estado = @Estado";
                AddParameter(command, "@Estado", estado.Value.ToString());
            }
            if (fechaDesde.HasValue) 
            { 
                sql += " AND FechaInspeccion >= @Desde"; 
                AddParameter(command, "@Desde", fechaDesde.Value.ToString("o")); 
            }
            if (fechaHasta.HasValue) 
            { 
                sql += " AND FechaInspeccion <= @Hasta"; 
                AddParameter(command, "@Hasta", fechaHasta.Value.ToString("o")); 
            }

            sql += " ORDER BY FechaInspeccion ASC LIMIT @PageSize OFFSET @Offset";
            AddParameter(command, "@PageSize", pageSize);
            AddParameter(command, "@Offset", (page - 1) * pageSize);

            command.CommandText = sql;
            using var reader = command.ExecuteReader();
            while (reader.Read()) list.Add(MapReaderToEntity(reader));

            return list.Select(c => c.ToModel()).OfType<Cita>().ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al filtrar citas en ADO.NET");
            return [];
        }
    }

    public Cita? GetById(Guid id)
    {
        try
        {
            EnsureConnectionOpen();
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT * FROM Citas WHERE Id = @Id";
            AddParameter(command, "@Id", id.ToString());

            using var reader = command.ExecuteReader();
            if (reader.Read()) return MapReaderToEntity(reader).ToModel();
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al buscar cita por ID {Id} en ADO.NET", id);
            return null;
        }
    }

    public bool ExisteCitaParaVehiculoEnFecha(string matricula, DateTime fecha)
    {
        try
        {
            EnsureConnectionOpen();
            using var command = _connection.CreateCommand();
            // Comparamos el inicio de la cadena de texto de la fecha (YYYY-MM-DD)
            command.CommandText = "SELECT COUNT(1) FROM Citas WHERE LOWER(VehiculoMatricula) = LOWER(@Matricula) AND FechaInspeccion LIKE @FechaDia AND IsDeleted = 0";
            AddParameter(command, "@Matricula", matricula);
            AddParameter(command, "@FechaDia", $"{fecha:yyyy-MM-dd}%");

            var count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al verificar cita repetida para vehículo {Matricula} en la fecha {Fecha}", matricula, fecha);
            return false;
        }
    }

    public int CountCitasPorDniYFecha(string dni, DateTime fecha)
    {
        try
        {
            EnsureConnectionOpen();
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM Citas WHERE LOWER(Dni) = LOWER(@Dni) AND FechaInspeccion LIKE @FechaDia AND IsDeleted = 0";
            AddParameter(command, "@Dni", dni);
            AddParameter(command, "@FechaDia", $"{fecha:yyyy-MM-dd}%");

            return Convert.ToInt32(command.ExecuteScalar());
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al contar citas del DNI {Dni} en la fecha {Fecha}", dni, fecha);
            return 0;
        }
    }

    public Result<Cita, DomainError> Create(Cita model)
    {
        model = model with { Id = Guid.NewGuid(), CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now, IsDeleted = false, DeletedAt = null };
        var entity = model.ToEntity();

        try
        {
            EnsureConnectionOpen();
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Citas (Id, Dni, VehiculoMatricula, FechaInspeccion, Estado, Observaciones, CreatedAt, UpdatedAt, IsDeleted, DeletedAt)
                VALUES (@Id, @Dni, @VehiculoMatricula, @FechaInspeccion, @Estado, @Observaciones, @CreatedAt, @UpdatedAt, @IsDeleted, @DeletedAt);";

            AddParameter(command, "@Id", entity!.Id.ToString());
            AddParameter(command, "@Dni", entity.Dni);
            AddParameter(command, "@VehiculoMatricula", entity.VehiculoMatricula);
            AddParameter(command, "@FechaInspeccion", entity.FechaInspeccion.ToString("o"));
            AddParameter(command, "@Estado", entity.Estado);
            AddParameter(command, "@Observaciones", entity.Observaciones);
            AddParameter(command, "@CreatedAt", entity.CreatedAt.ToString("o"));
            AddParameter(command, "@UpdatedAt", entity.UpdatedAt.ToString("o"));
            AddParameter(command, "@IsDeleted", entity.IsDeleted ? 1 : 0);
            AddParameter(command, "@DeletedAt", entity.DeletedAt?.ToString("o") ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
            return Result.Success<Cita, DomainError>(GetById(entity.Id)!);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al crear cita en ADO.NET");
            return Result.Failure<Cita, DomainError>(new DomainError(ex.Message, "DATABASE_ERROR"));
        }
    }

    public Result<Cita, DomainError> Update(Guid id, Cita model)
    {
        try
        {
            var existing = GetById(id);
            if (existing == null) return Result.Failure<Cita, DomainError>(DomainError.NotFound("Cita", id.ToString()));

            model = model with { Id = id, CreatedAt = existing.CreatedAt, UpdatedAt = DateTime.Now };
            var entity = model.ToEntity();

            EnsureConnectionOpen();
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                UPDATE Citas SET 
                    Dni = @Dni, VehiculoMatricula = @VehiculoMatricula, 
                    FechaInspeccion = @FechaInspeccion, Estado = @Estado, 
                    Observaciones = @Observaciones, UpdatedAt = @UpdatedAt
                WHERE Id = @Id";

            AddParameter(command, "@Id", id.ToString());
            AddParameter(command, "@Dni", entity!.Dni);
            AddParameter(command, "@VehiculoMatricula", entity.VehiculoMatricula);
            AddParameter(command, "@FechaInspeccion", entity.FechaInspeccion.ToString("o"));
            AddParameter(command, "@Estado", entity.Estado);
            AddParameter(command, "@Observaciones", entity.Observaciones);
            AddParameter(command, "@UpdatedAt", entity.UpdatedAt.ToString("o"));

            command.ExecuteNonQuery();
            return Result.Success<Cita, DomainError>(GetById(id)!);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al actualizar cita con ID {Id} en ADO.NET", id);
            return Result.Failure<Cita, DomainError>(new DomainError(ex.Message, "DATABASE_ERROR"));
        }
    }

    public Cita? Delete(Guid id, bool isLogical = true)
    {
        try
        {
            var existing = GetById(id);
            if (existing == null) return null;

            EnsureConnectionOpen();
            using var command = _connection.CreateCommand();

            if (isLogical)
            {
                command.CommandText = "UPDATE Citas SET IsDeleted = 1, DeletedAt = @DeletedAt, UpdatedAt = @UpdatedAt WHERE Id = @Id";
                AddParameter(command, "@Id", id.ToString());
                AddParameter(command, "@DeletedAt", DateTime.Now.ToString("o"));
                AddParameter(command, "@UpdatedAt", DateTime.Now.ToString("o"));
                command.ExecuteNonQuery();
                return GetById(id);
            }
            else
            {
                command.CommandText = "DELETE FROM Citas WHERE Id = @Id";
                AddParameter(command, "@Id", id.ToString());
                command.ExecuteNonQuery();
                return existing;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al eliminar cita con ID {Id} en ADO.NET", id);
            return null;
        }
    }

    public Result<Cita, DomainError> Restore(Guid id)
    {
        try
        {
            var existing = GetById(id);
            if (existing == null) return Result.Failure<Cita, DomainError>(DomainError.NotFound("Cita", id.ToString()));

            EnsureConnectionOpen();
            using var command = _connection.CreateCommand();
            command.CommandText = "UPDATE Citas SET IsDeleted = 0, DeletedAt = NULL, UpdatedAt = @UpdatedAt WHERE Id = @Id";
            AddParameter(command, "@Id", id.ToString());
            AddParameter(command, "@UpdatedAt", DateTime.Now.ToString("o"));

            command.ExecuteNonQuery();
            return Result.Success<Cita, DomainError>(GetById(id)!);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al restaurar cita con ID {Id} en ADO.NET", id);
            return Result.Failure<Cita, DomainError>(new DomainError(ex.Message, "DATABASE_ERROR"));
        }
    }

    public int Count(bool includeDeleted = false)
    {
        EnsureConnectionOpen();
        using var command = _connection.CreateCommand();
        command.CommandText = includeDeleted ? "SELECT COUNT(1) FROM Citas" : "SELECT COUNT(1) FROM Citas WHERE IsDeleted = 0";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private int CountTotal()
    {
        EnsureConnectionOpen();
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM Citas";
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

    private CitaEntity MapReaderToEntity(IDataReader reader)
    {
        return new CitaEntity {
            Id = Guid.Parse(reader["Id"].ToString()!),
            Dni = reader["Dni"].ToString()!,
            VehiculoMatricula = reader["VehiculoMatricula"].ToString()!,
            FechaInspeccion = DateTime.Parse(reader["FechaInspeccion"].ToString()!),
            Estado = reader["Estado"].ToString()!,
            Observaciones = reader["Observaciones"] != DBNull.Value ? reader["Observaciones"].ToString()! : string.Empty,
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
        if (dropData) { command.CommandText = "DROP TABLE IF EXISTS Citas"; command.ExecuteNonQuery(); }

        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Citas (
                Id TEXT PRIMARY KEY,
                Dni TEXT NOT NULL,
                VehiculoMatricula TEXT NOT NULL,
                FechaInspeccion TEXT NOT NULL,
                Estado TEXT NOT NULL,
                Observaciones TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                DeletedAt TEXT
            );";
        command.ExecuteNonQuery();
    }

    private void Seed()
    {
        foreach (var c in CitaFactory.Seed()) Create(c);
    }
}