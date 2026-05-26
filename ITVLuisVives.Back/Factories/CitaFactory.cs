using System;
using System.Collections.Generic;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Models;

namespace ITVLuisVives.Back.Factories;

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
    ///     Genera el conjunto de datos iniciales (semillas) para las citas de forma automatizada.
    /// </summary>
    public static IEnumerable<Cita> Seed()
    {
        var listaSemilla = new List<Cita>
        {
            CrearNueva("12345678A", "1234BBB", DateTime.Today.AddHours(10), "Revisión periódica"),
            CrearNueva("87654321B", "5678CXC", DateTime.Today.AddHours(10), "Fallo leve en luces"),
            CrearNueva("11111111C", "9012FFF", DateTime.Today.AddHours(10), "Primera inspección técnica")
        };

        var random = new Random();
        string letrasDni = "TRWAGMYFPDXBNJZSQVHLCKE";
        string letrasMatriculaValidas = "BCDFGHJKLMNPQRSTVWXYZ";
        string[] catalogoObservaciones = new[]
        {
            "Inspección periódica",
            "Transferencia titular",
            "Fallo leve en luces",
            "Revisión semestral",
            "Homologación reforma",
            "Segunda inspección",
            "Revisión voluntaria",
            "Control de emisiones",
            "Duplicado de tarjeta",
            "Viaje largo recorrido",
            "Vehículo histórico",
            "Cambio de neumáticos"
        };

        for (int i = 1; i <= 55; i++)
        {
            int numeroDni = random.Next(10000000, 99999999);
            char letraDni = letrasDni[numeroDni % 23];
            string dniValido = $"{numeroDni}{letraDni}";

            int numeroMatricula = random.Next(1000, 9999);
            string letrasMatricula = $"{letrasMatriculaValidas[random.Next(letrasMatriculaValidas.Length)]}" +
                                     $"{letrasMatriculaValidas[random.Next(letrasMatriculaValidas.Length)]}" +
                                     $"{letrasMatriculaValidas[random.Next(letrasMatriculaValidas.Length)]}";
            string matriculaValida = $"{numeroMatricula:D4}{letrasMatricula}";

            DateTime fechaBase = DateTime.Today.AddDays(random.Next(1, 20));
            TimeSpan horaAleatoria = TimeSpan.FromHours(random.Next(8, 19)) + 
                                     TimeSpan.FromMinutes(random.Next(0, 2) * 30);
            
            DateTime fechaYHoraFinal = fechaBase + horaAleatoria;
            
            string observacionAleatoria = catalogoObservaciones[random.Next(catalogoObservaciones.Length)];

            var citaAleatoria = CrearNueva(
                dniValido, 
                matriculaValida, 
                fechaYHoraFinal, 
                observacionAleatoria
            );

            listaSemilla.Add(citaAleatoria);
        }

        return listaSemilla;
    }
}