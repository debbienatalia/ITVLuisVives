using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ITVLuisVives.Back.Dto;
using ITVLuisVives.Back.Entity;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Models;

namespace ITVLuisVives.Back.Mappers;

public static class CitaMapper
{
    private const string DateTimeFormat = "s";
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    public static Cita? ToModel(this CitaEntity? entity)
    {
        if (entity == null) return null;

        return new Cita
        {
            Id = entity.Id,
            Dni = entity.Dni,
            VehiculoMatricula = entity.VehiculoMatricula,
            FechaInspeccion = entity.FechaInspeccion,
            Estado = Enum.TryParse(entity.Estado, out EstadoCita estado) ? estado : EstadoCita.Pendiente,
            Observaciones = entity.Observaciones,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };
    }

    public static IEnumerable<Cita> ToModel(this IEnumerable<CitaEntity> entities)
    {
        return entities.Select(ToModel).OfType<Cita>();
    }

    public static CitaEntity ToEntity(this Cita model)
    {
        return new CitaEntity
        {
            Id = model.Id,
            Dni = model.Dni ?? string.Empty,
            VehiculoMatricula = model.VehiculoMatricula ?? string.Empty,
            FechaInspeccion = model.FechaInspeccion,
            Estado = model.Estado.ToString(),
            Observaciones = model.Observaciones ?? string.Empty,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            IsDeleted = model.IsDeleted,
            DeletedAt = model.DeletedAt
        };
    }

    public static CitaDto ToDto(this Cita model)
    {
        return new CitaDto(
            model.Id.ToString(),
            model.Dni ?? string.Empty,
            model.VehiculoMatricula ?? string.Empty,
            model.FechaInspeccion.ToString(DateTimeFormat, InvariantCulture),
            model.Estado.ToString(),
            model.Observaciones ?? string.Empty,
            model.EsHoy, // Mapeamos el valor booleano calculado en tu modelo
            model.CreatedAt.ToString(DateTimeFormat, InvariantCulture),
            model.UpdatedAt.ToString(DateTimeFormat, InvariantCulture),
            model.IsDeleted,
            model.DeletedAt?.ToString(DateTimeFormat, InvariantCulture)
        );
    }

    public static Cita ToModel(this CitaDto dto)
    {
        var createdAt = DateTime.Parse(dto.CreatedAt, InvariantCulture);
        var updatedAt = DateTime.Parse(dto.UpdatedAt, InvariantCulture);
        DateTime? deletedAt = string.IsNullOrEmpty(dto.DeletedAt)
            ? null
            : DateTime.Parse(dto.DeletedAt, InvariantCulture);
        var fechaInspeccion = DateTime.Parse(dto.FechaInspeccion, InvariantCulture);

        return new Cita
        {
            Id = Guid.TryParse(dto.Id, out var guidId) ? guidId : Guid.NewGuid(),
            Dni = dto.Dni,
            VehiculoMatricula = dto.VehiculoMatricula,
            FechaInspeccion = fechaInspeccion,
            Estado = Enum.TryParse(dto.Estado, out EstadoCita estado) ? estado : EstadoCita.Pendiente,
            Observaciones = dto.Observaciones,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = dto.IsDeleted,
            DeletedAt = deletedAt
        };
    }
}