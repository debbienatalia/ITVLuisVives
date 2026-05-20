using System;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Models;

namespace ITVLuisVives.Back.Factories.Vehiculos;

/// <summary>
///     Factoría encargada de la instanciación segura de nuevos modelos de Vehículo.
/// </summary>
public static class VehiculoFactory
{
    /// <summary>
    ///     Crea un vehículo nuevo listo para el negocio con los sellos de auditoría inicializados.
    /// </summary>
    public static Vehiculo CrearNuevo(string matricula, string marca, string modelo, TipoMotor motor, DateTime fechaMatriculacion)
    {
        var ahora = DateTime.Now;
        return new Vehiculo
        {
            Id = 0,
            Matricula = matricula.Trim().ToUpper(),
            Marca = marca.Trim(),
            Modelo = modelo.Trim(),
            Motor = motor,
            FechaMatriculacion = fechaMatriculacion,
            CreatedAt = ahora,
            UpdatedAt = ahora,
            IsDeleted = false,
            DeletedAt = null
        };
    }
}