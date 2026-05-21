using System;
using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using ITVLuisVives.Back.Entity;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Mappers;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Factories.Vehiculos;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ITVLuisVives.Back.Repositories;

/// <summary>
///     Repositorio de vehículos que utiliza Entity Framework Core con SQLite.
/// </summary>
public class VehiculoEfRepository : IVehiculoRepository
{
    private readonly ItvDbContext _context;
    private readonly ILogger _logger = Log.ForContext<VehiculoEfRepository>();

    public VehiculoEfRepository(ItvDbContext context, bool dropData = false, bool seedData = false)
    {
        _context = context;

        if (dropData) 
            _context.Database.EnsureDeleted();

        _context.Database.EnsureCreated();

        if (seedData && !_context.Vehiculos.Any())
        {
            _logger.Information("Sembrando datos iniciales de vehículos...");
            Seed();
        }
    }

    public IEnumerable<Vehiculo> GetFiltered(string? matricula, string? marca, TipoMotor? motor, DateTime? matriculacionDesde, DateTime? matriculacionHasta, int page = 1, int pageSize = 10, bool includeDeleted = false)
    {
        try
        {
            var query = includeDeleted 
                ? _context.Vehiculos.AsNoTracking() 
                : _context.Vehiculos.Where(v => !v.IsDeleted).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(matricula))
            {
                query = query.Where(v => v.Matricula.ToLower().Contains(matricula.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(marca))
            {
                query = query.Where(v => v.Marca.ToLower().Contains(marca.ToLower()));
            }

            if (motor.HasValue)
            {
                var motorStr = motor.Value.ToString();
                query = query.Where(v => v.Motor == motorStr);
            }

            if (matriculacionDesde.HasValue)
            {
                query = query.Where(v => v.FechaMatriculacion >= matriculacionDesde.Value);
            }

            if (matriculacionHasta.HasValue)
            {
                query = query.Where(v => v.FechaMatriculacion <= matriculacionHasta.Value);
            }

            var entities = query
                .OrderBy(v => v.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return entities.Select(e => e.ToModel()).OfType<Vehiculo>().ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al obtener vehículos filtrados");
            return Enumerable.Empty<Vehiculo>();
        }
    }

    public Vehiculo? GetById(int id)
    {
        try
        {
            var entity = _context.Vehiculos.AsNoTracking().FirstOrDefault(v => v.Id == id);
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
            var entity = _context.Vehiculos.AsNoTracking()
                .FirstOrDefault(v => v.Matricula.ToLower() == matricula.ToLower());
            return entity?.ToModel();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al obtener vehículo por matrícula {Matricula}", matricula);
            return null;
        }
    }

    public bool ExisteMatricula(string matricula)
    {
        try
        {
            return _context.Vehiculos.Any(v => v.Matricula.ToLower() == matricula.ToLower() && !v.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al verificar existencia de la matrícula {Matricula}", matricula);
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

        try
        {
            var entity = model.ToEntity();
            _context.Vehiculos.Add(entity);
            _context.SaveChanges();

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
        var entity = _context.Vehiculos.FirstOrDefault(v => v.Id == id);
        if (entity == null)
            return Result.Failure<Vehiculo, DomainError>(DomainError.NotFound("Vehiculo", id.ToString()));

        if (model.Matricula.ToLower() != entity.Matricula.ToLower() && _context.Vehiculos.Any(v => v.Matricula.ToLower() == model.Matricula.ToLower() && v.Id != id))
            return Result.Failure<Vehiculo, DomainError>(new DomainError($"La matrícula '{model.Matricula}' ya pertenece a otro vehículo.", "MATRICULA_DUPLICADA"));

        entity.Matricula = model.Matricula.Trim().ToUpper();
        entity.Marca = model.Marca.Trim();
        entity.Modelo = model.Modelo.Trim();
        entity.Motor = model.Motor.ToString();
        entity.FechaMatriculacion = model.FechaMatriculacion;
        entity.UpdatedAt = DateTime.Now;

        try
        {
            _context.SaveChanges();
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
            var entity = _context.Vehiculos.FirstOrDefault(v => v.Id == id);
            if (entity == null) return null;

            if (isLogical)
            {
                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.Now;
                entity.UpdatedAt = DateTime.Now;
                _context.SaveChanges();
                return GetById(id);
            }

            _context.Vehiculos.Remove(entity);
            _context.SaveChanges();
            return entity.ToModel();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al eliminar vehículo con ID {Id}", id);
            return null;
        }
    }

    public Result<Vehiculo, DomainError> Restore(int id)
    {
        try
        {
            var entity = _context.Vehiculos.FirstOrDefault(v => v.Id == id);
            if (entity == null)
                return Result.Failure<Vehiculo, DomainError>(DomainError.NotFound("Vehiculo", id.ToString()));

            entity.IsDeleted = false;
            entity.DeletedAt = null;
            entity.UpdatedAt = DateTime.Now;
            _context.SaveChanges();

            _logger.Information("Vehículo con ID {Id} restaurado correctamente", id);
            return Result.Success<Vehiculo, DomainError>(entity.ToModel()!);
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
            var query = includeDeleted ? _context.Vehiculos : _context.Vehiculos.Where(v => !v.IsDeleted);
            return query.Count();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al contar vehículos");
            return 0;
        }
    }

    private void Seed()
    {
        foreach (var v in VehiculoFactory.Seed())
        {
            Create(v);
        }
    }
}