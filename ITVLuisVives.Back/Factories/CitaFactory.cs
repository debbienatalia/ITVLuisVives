using System;
using System.Collections.Generic;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Models;

namespace ITVLuisVives.Back.Factories.Citas;

/// <summary>
///     Factoría encargada de la instanciación segura de nuevos modelos de Cita.
/// </summary>
public static class CitaFactory
{
    /// <summary>
    ///     Fabrica una cita de inspección con un ID tipo GUID único global y estado inicial 'Pendiente' en hora local.
    /// </summary>
    public static Cita CrearNueva(string dni, string matricula, DateTime fechaInspeccion, string observaciones = "")
    {
        var ahora = DateTime.Now;
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

    /// <summary>
    ///     Genera el conjunto de datos iniciales (semillas) para las citas.
    /// </summary>
    public static IEnumerable<Cita> Seed()
    {
        return new List<Cita>
        {
            CrearNueva("12345678A", "1234BBB", DateTime.Now.AddDays(2), "Revisión periódica"),
            CrearNueva("87654321B", "5678CXC", DateTime.Now.AddDays(5), "Fallo leve en luces"),
            CrearNueva("11111111C", "9012FFF", DateTime.Now.AddDays(7), "Primera inspección técnica")
        };
    }
}