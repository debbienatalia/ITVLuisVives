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
///     Validador de dominio para la entidad <see cref="Vehiculo" />.
/// </summary>
public class VehiculoValidator : IValidator<Vehiculo>
{
    private static readonly Regex MatriculaRegex = new(@"^[0-9]{4}[A-Z]{3}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public Result<Vehiculo, DomainError> Validar(Vehiculo vehiculo)
    {
        if (vehiculo == null)
            return Result.Failure<Vehiculo, DomainError>(new DomainError("El objeto vehículo no puede ser nulo.", "VALIDATION_ERROR"));

        var errores = new List<string>();

        string matriculaLimpia = vehiculo.Matricula?.Trim().Replace(" ", "").Replace("-", "") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(vehiculo.Matricula) || !MatriculaRegex.IsMatch(matriculaLimpia))
        {
            errores.Add("La matrícula no es válida (4 números y 3 letras).");
        }

        if (string.IsNullOrWhiteSpace(vehiculo.Marca) || vehiculo.Marca.Trim().Length < 2 || vehiculo.Marca.Length > 50)
        {
            errores.Add("La marca es obligatoria (2-50 caracteres.).");
        }

        if (string.IsNullOrWhiteSpace(vehiculo.Modelo) || vehiculo.Modelo.Trim().Length < 1 || vehiculo.Modelo.Length > 50)
        {
            errores.Add("El modelo es obligatorio (1-50 caracteres.).");
        }

        if (!Enum.IsDefined(typeof(TipoMotor), vehiculo.Motor))
        {
            errores.Add("El tipo de motor proporcionado no es válido.");
        }

        if (vehiculo.FechaMatriculacion == default || vehiculo.FechaMatriculacion > DateTime.Today)
        {
            errores.Add("La fecha de matriculación no puede ser una fecha futura.");
        }

        if (errores.Any())
        {
            return Result.Failure<Vehiculo, DomainError>(new DomainError(string.Join(" ", errores), "VALIDATION_ERROR"));
        }

        return Result.Success<Vehiculo, DomainError>(vehiculo);
    }
}