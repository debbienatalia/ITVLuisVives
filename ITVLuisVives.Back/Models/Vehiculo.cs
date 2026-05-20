using System;
using ITVLuisVives.Back.Enums;

namespace ITVLuisVives.Back.Models;

/// <summary>
///     Representa un vehículo registrado para inspección en el sistema.
/// </summary>
public record Vehiculo {
    public int Id { get; init; }
    public string Matricula { get; init; } = string.Empty;
    public string Marca { get; init; } = string.Empty;
    public string Modelo { get; init; } = string.Empty;
    public TipoMotor Motor { get; init; }
    public DateTime FechaMatriculacion { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public DateTime UpdatedAt { get; init; } = DateTime.Now;
    public bool IsDeleted { get; init; } = false;
    public DateTime? DeletedAt { get; init; } = null;

    public int AntiguedadAnios => DateTime.Today.Year - FechaMatriculacion.Year;

    public virtual bool Equals(Vehiculo? other) {
        return other is not null && string.Equals(Matricula, other.Matricula, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Matricula.ToLowerInvariant());
    }
}