namespace ITVLuisVives.Back.Dto;

/// <summary>
///     Objeto de transferencia de datos de un Vehículo adaptado para la vista WPF.
/// </summary>
public record VehiculoDto(
    string Matricula,
    string Marca,
    string Modelo,
    string Motor,
    string FechaMatriculacion,
    int AntiguedadAnios
);