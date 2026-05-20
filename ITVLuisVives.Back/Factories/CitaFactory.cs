using System;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Models;

namespace ITVLuisVives.Back.Factories.Citas;

/// <summary>
///     Factoría encargada de la instanciación segura de nuevos modelos de Cita.
/// </summary>
public static class CitaFactory
{
    /// <summary>
    ///     Fabrica una cita de inspección con un ID tipo GUID único global y estado inicial 'Pendiente'.
    /// </summary>
    public static Cita CrearNueva(string dni, string matricula, DateTime fechaInspeccion, string observaciones = "")
    {
        var ahora = DateTime.UtcNow;
        return new Cita
        {
            Id = Guid.NewGuid(),
            Dni = dni.Trim().ToUpper(),
            VehiculoMatricula = matricula.Trim().ToUpper(),
            FechaInspeccion = fechaInspeccion,
            Estado = EstadoCita.Pendiente,
            Observaciones = observaciones.Trim(),
            CreatedAt = ahora,
            UpdatedAt = ahora,
            IsDeleted = false,
            DeletedAt = null
        };
    }
}