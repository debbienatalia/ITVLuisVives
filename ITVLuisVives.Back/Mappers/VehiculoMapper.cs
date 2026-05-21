using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ITVLuisVives.Back.Dto;
using ITVLuisVives.Back.Entity;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Models;

namespace ITVLuisVives.Back.Mappers;

public static class VehiculoMapper
{
    private const string DateFormat = "d";
    private const string DateTimeFormat = "s";
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    public static Vehiculo? ToModel(this VehiculoEntity? entity)
    {
        if (entity == null) return null;

        return new Vehiculo
        {
            Id = entity.Id,
            Matricula = entity.Matricula,
            Marca = entity.Marca,
            Modelo = entity.Modelo,
            Motor = Enum.TryParse(entity.Motor, out TipoMotor motor) ? motor : TipoMotor.Gasolina,
            FechaMatriculacion = entity.FechaMatriculacion,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };
    }

    public static IEnumerable<Vehiculo> ToModel(this IEnumerable<VehiculoEntity> entities)
    {
        return entities.Select(ToModel).OfType<Vehiculo>();
    }

    public static VehiculoEntity ToEntity(this Vehiculo model)
    {
        return new VehiculoEntity
        {
            Id = model.Id,
            Matricula = model.Matricula ?? string.Empty,
            Marca = model.Marca ?? string.Empty,
            Modelo = model.Modelo ?? string.Empty,
            Motor = model.Motor.ToString(),
            FechaMatriculacion = model.FechaMatriculacion,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            IsDeleted = model.IsDeleted,
            DeletedAt = model.DeletedAt
        };
    }

    public static VehiculoDto ToDto(this Vehiculo model)
    {
        return new VehiculoDto(
            model.Id,
            model.Matricula ?? string.Empty,
            model.Marca ?? string.Empty,
            model.Modelo ?? string.Empty,
            model.Motor.ToString(),
            model.FechaMatriculacion.ToString(DateFormat, InvariantCulture),
            model.AntiguedadAnios, // Se lee directamente de tu propiedad calculada
            model.CreatedAt.ToString(DateTimeFormat, InvariantCulture),
            model.UpdatedAt.ToString(DateTimeFormat, InvariantCulture),
            model.IsDeleted,
            model.DeletedAt?.ToString(DateTimeFormat, InvariantCulture)
        );
    }

    public static Vehiculo ToModel(this VehiculoDto dto)
    {
        var createdAt = DateTime.Parse(dto.CreatedAt, InvariantCulture);
        var updatedAt = DateTime.Parse(dto.UpdatedAt, InvariantCulture);
        DateTime? deletedAt = string.IsNullOrEmpty(dto.DeletedAt)
            ? null
            : DateTime.Parse(dto.DeletedAt, InvariantCulture);
        var fechaMatriculacion = DateTime.Parse(dto.FechaMatriculacion, InvariantCulture);

        return new Vehiculo
        {
            Id = dto.Id,
            Matricula = dto.Matricula,
            Marca = dto.Marca,
            Modelo = dto.Modelo,
            Motor = Enum.TryParse(dto.Motor, out TipoMotor motor) ? motor : TipoMotor.Gasolina,
            FechaMatriculacion = fechaMatriculacion,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = dto.IsDeleted,
            DeletedAt = deletedAt
        };
    }
}