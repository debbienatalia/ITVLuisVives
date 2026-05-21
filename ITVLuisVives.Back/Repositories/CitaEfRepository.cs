using System;
using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using ITVLuisVives.Back.Entity;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Mappers;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Factories.Citas;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ITVLuisVives.Back.Repositories;

/// <summary>
///     Repositorio de citas que utiliza Entity Framework Core con SQLite.
/// </summary>
public class CitaEfRepository : ICitaRepository
{
    private readonly ItvDbContext _context;
    private readonly ILogger _logger = Log.ForContext<CitaEfRepository>();

    public CitaEfRepository(ItvDbContext context, bool dropData = false, bool seedData = false)
    {
        _context = context;

        if (dropData) 
            _context.Database.EnsureDeleted();

        _context.Database.EnsureCreated();

        if (seedData && !_context.Citas.Any())
        {
            _logger.Information("Sembrando datos iniciales de citas en EF Core...");
            Seed();
        }
    }

    public IEnumerable<Cita> GetFiltered(string? dni, string? matricula, EstadoCita? estado, DateTime? fechaDesde, DateTime? fechaHasta, int page = 1, int pageSize = 10, bool includeDeleted = false)
    {
        try
        {
            var query = includeDeleted 
                ? _context.Citas.AsNoTracking() 
                : _context.Citas.Where(c => !c.IsDeleted).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(dni)) 
                query = query.Where(c => c.Dni.ToLower().Contains(dni.ToLower()));

            if (!string.IsNullOrWhiteSpace(matricula)) 
                query = query.Where(c => c.VehiculoMatricula.ToLower().Contains(matricula.ToLower()));

            if (estado.HasValue)
            {
                var estadoStr = estado.Value.ToString();
                query = query.Where(c => c.Estado == estadoStr);
            }

            if (fechaDesde.HasValue) 
                query = query.Where(c => c.FechaInspeccion >= fechaDesde.Value);

            if (fechaHasta.HasValue) 
                query = query.Where(c => c.FechaInspeccion <= fechaHasta.Value);

            var entities = query
                .OrderBy(c => c.FechaInspeccion)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return entities.Select(e => e.ToModel()).OfType<Cita>().ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al obtener citas filtradas en EF Core");
            return Enumerable.Empty<Cita>();
        }
    }

    public Cita? GetById(Guid id)
    {
        try
        {
            var entity = _context.Citas.AsNoTracking().FirstOrDefault(c => c.Id == id);
            return entity?.ToModel();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al obtener cita por ID {Id} en EF Core", id);
            return null;
        }
    }

    public bool ExisteCitaParaVehiculoEnFecha(string matricula, DateTime fecha)
    {
        try
        {
            var targetDate = fecha.Date;
            return _context.Citas.Any(c => 
                !c.IsDeleted && 
                c.VehiculoMatricula.ToLower() == matricula.ToLower() && 
                c.FechaInspeccion.Date == targetDate);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al verificar existencia de cita para vehículo {Matricula} en la fecha {Fecha}", matricula, fecha);
            return false;
        }
    }

    public int CountCitasPorDniYFecha(string dni, DateTime fecha)
    {
        try
        {
            var targetDate = fecha.Date;
            return _context.Citas.Count(c => 
                !c.IsDeleted && 
                c.Dni.ToLower() == dni.ToLower() && 
                c.FechaInspeccion.Date == targetDate);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al contar citas para el DNI {Dni} en la fecha {Fecha}", dni, fecha);
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

        try
        {
            var entity = model.ToEntity();
            if (entity == null) return Result.Failure<Cita, DomainError>(new DomainError("Error en el mapeo", "MAPPER_ERROR"));

            _context.Citas.Add(entity);
            _context.SaveChanges();

            return Result.Success<Cita, DomainError>(GetById(entity.Id)!);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al crear cita en EF Core");
            return Result.Failure<Cita, DomainError>(new DomainError(ex.Message, "DATABASE_ERROR"));
        }
    }

    public Result<Cita, DomainError> Update(Guid id, Cita model)
    {
        var entity = _context.Citas.FirstOrDefault(c => c.Id == id);
        if (entity == null)
            return Result.Failure<Cita, DomainError>(DomainError.NotFound("Cita", id.ToString()));

        entity.Dni = model.Dni.Trim().ToUpper();
        entity.VehiculoMatricula = model.VehiculoMatricula.Trim().ToUpper();
        entity.FechaInspeccion = model.FechaInspeccion;
        entity.Estado = model.Estado.ToString();
        entity.Observaciones = model.Observaciones.Trim();
        entity.UpdatedAt = DateTime.Now;

        try
        {
            _context.SaveChanges();
            return Result.Success<Cita, DomainError>(GetById(id)!);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al actualizar cita con ID {Id} en EF Core", id);
            return Result.Failure<Cita, DomainError>(new DomainError(ex.Message, "DATABASE_ERROR"));
        }
    }

    public Cita? Delete(Guid id, bool isLogical = true)
    {
        try
        {
            var entity = _context.Citas.FirstOrDefault(c => c.Id == id);
            if (entity == null) return null;

            if (isLogical)
            {
                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.Now;
                entity.UpdatedAt = DateTime.Now;
                _context.SaveChanges();
                return GetById(id);
            }

            _context.Citas.Remove(entity);
            _context.SaveChanges();
            return entity.ToModel();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al eliminar cita con ID {Id} en EF Core", id);
            return null;
        }
    }

    public Result<Cita, DomainError> Restore(Guid id)
    {
        try
        {
            var entity = _context.Citas.FirstOrDefault(c => c.Id == id);
            if (entity == null)
                return Result.Failure<Cita, DomainError>(DomainError.NotFound("Cita", id.ToString()));

            entity.IsDeleted = false;
            entity.DeletedAt = null;
            entity.UpdatedAt = DateTime.Now;
            _context.SaveChanges();

            return Result.Success<Cita, DomainError>(entity.ToModel()!);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al restaurar cita con ID {Id} en EF Core", id);
            return Result.Failure<Cita, DomainError>(new DomainError(ex.Message, "DATABASE_ERROR"));
        }
    }

    public int Count(bool includeDeleted = false)
    {
        try
        {
            var query = includeDeleted ? _context.Citas : _context.Citas.Where(c => !c.IsDeleted);
            return query.Count();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al contar citas en EF Core");
            return 0;
        }
    }

    private void Seed()
    {
        foreach (var c in CitaFactory.Seed())
        {
            Create(c);
        }
    }
}