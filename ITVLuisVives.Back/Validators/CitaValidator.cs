using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Enums;

namespace ITVLuisVives.Back.Validators;

/// <summary>
///     Validador de dominio para la entidad <see cref="Cita" />.
/// </summary>
public class CitaValidator : IValidator<Cita>
{
    private static readonly Regex MatriculaRegex = new(@"^[0-9]{4}[A-Z]{3}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DniRegex = new(@"^[0-9]{8}[A-Z]$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private const string LetrasDniValidas = "TRWAGMYFPDXBNJZSQVHLCKE";

    public Result<Cita, DomainError> Validar(Cita cita)
    {
        if (cita == null)
            return Result.Failure<Cita, DomainError>(new DomainError("El objeto cita no puede ser nulo.", "VALIDATION_ERROR"));

        var errores = new List<string>();

        string dniLimpio = cita.Dni?.Trim().ToUpper().Replace(" ", "").Replace("-", "") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cita.Dni) || !DniRegex.IsMatch(dniLimpio))
        {
            errores.Add("El DNI no tiene un formato válido (8 números y 1 letra).");
        }
        else
        {
            string numeroStr = dniLimpio.Substring(0, 8);
            char letraIndice = dniLimpio[8];
            
            if (int.TryParse(numeroStr, out int dniNumerico))
            {
                char letraCalculada = LetrasDniValidas[dniNumerico % 23];
                if (letraIndice != letraCalculada)
                {
                    errores.Add("La letra del DNI no coincide con el dígito de control oficial.");
                }
            }
        }

        string matriculaLimpia = cita.VehiculoMatricula?.Trim().Replace(" ", "").Replace("-", "") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cita.VehiculoMatricula) || !MatriculaRegex.IsMatch(matriculaLimpia))
        {
            errores.Add("La matrícula del vehículo no es válida (4 números y 3 letras).");
        }

        DateTime fechaMinima = DateTime.Today;
        DateTime fechaMaxima = DateTime.Today.AddDays(30);
        
        if (cita.FechaInspeccion == default)
        {
            errores.Add("La fecha de inspección es obligatoria.");
        }
        else if (cita.FechaInspeccion.Date < fechaMinima || cita.FechaInspeccion.Date > fechaMaxima)
        {
            errores.Add("La fecha de inspección debe programarse entre el día de hoy y los próximos 30 días naturales.");
        }

        if (!Enum.IsDefined(typeof(EstadoCita), cita.Estado))
        {
            errores.Add("El estado asignado a la cita no es un estado válido del sistema.");
        }

        if (cita.Observaciones != null && cita.Observaciones.Length > 500)
        {
            errores.Add("Las observaciones no pueden superar los 500 caracteres.");
        }

        if (errores.Any())
        {
            return Result.Failure<Cita, DomainError>(new DomainError(string.Join(" ", errores), "VALIDATION_ERROR"));
        }

        return Result.Success<Cita, DomainError>(cita);
    }
}