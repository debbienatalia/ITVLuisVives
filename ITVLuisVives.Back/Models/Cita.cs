using System;
using ITVLuisVives.Back.Enums;

namespace ITVLuisVives.Back.Models;

/// <summary>
///     Representa una cita de inspección técnica programada.
/// </summary>
public record Cita {
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Dni { get; init; } = string.Empty;
    public string VehiculoMatricula { get; init; } = string.Empty;
    public DateTime FechaInspeccion { get; init; }
    public EstadoCita Estado { get; init; } = EstadoCita.Pendiente;
    public string Observaciones { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public DateTime UpdatedAt { get; init; } = DateTime.Now;
    public bool IsDeleted { get; init; } = false;
    public DateTime? DeletedAt { get; init; } = null;

    public bool EsHoy => FechaInspeccion.Date == DateTime.Today;

    public virtual bool Equals(Cita? other) {
        return other is not null && Id.Equals(other.Id);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Id);
    }
}