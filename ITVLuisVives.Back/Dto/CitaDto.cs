namespace ITVLuisVives.Back.Dto;

/// <summary>
///     Objeto de transferencia de datos de una Cita adaptado para la vista WPF.
/// </summary>
public record CitaDto(
    string Id,
    string Dni,
    string VehiculoMatricula,
    string FechaInspeccion,
    string Estado,
    string Observaciones,
    bool EsHoy,
    string CreatedAt,
    string UpdatedAt,
    bool IsDeleted,
    string? DeletedAt
);