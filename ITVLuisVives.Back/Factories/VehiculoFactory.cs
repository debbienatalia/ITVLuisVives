using System;
using System.Collections.Generic;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Models;

namespace ITVLuisVives.Back.Factories.Vehiculos;

/// <summary>
///     Factoría encargada de la instanciación segura de nuevos modelos de Vehículo y datos iniciales.
/// </summary>
public static class VehiculoFactory
{
    /// <summary>
    ///     Crea un vehículo nuevo listo para el negocio con los sellos de auditoría inicializados en hora local.
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

    /// <summary>
    ///     Genera el conjunto de datos iniciales (semillas) exigido por los repositorios.
    /// </summary>
    public static IEnumerable<Vehiculo> Seed()
    {
        return new List<Vehiculo>
        {
            CrearNuevo("1234BBB", "Seat", "Ibiza", TipoMotor.Gasolina, new DateTime(2018, 5, 20)),
            CrearNuevo("5678CXC", "Toyota", "Yaris", TipoMotor.Híbrido, new DateTime(2021, 11, 14)),
            CrearNuevo("9012FFF", "Tesla", "Model 3", TipoMotor.Eléctrico, new DateTime(2023, 3, 5)),
            CrearNuevo("3456GGG", "Volkswagen", "Golf", TipoMotor.Diesel, new DateTime(2015, 8, 1))
        };
    }
}