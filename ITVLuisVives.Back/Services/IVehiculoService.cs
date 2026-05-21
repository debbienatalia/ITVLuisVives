using System;
using System.Collections.Generic;
using CSharpFunctionalExtensions;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Enums;

namespace ITVLuisVives.Back.Services.Vehiculos;

/// <summary>
///     Define el contrato de negocio para la gestión de vehículos dentro de la ITV.
/// </summary>
public interface IVehiculoService
{
    /// <summary>
    ///     Obtiene una lista paginada y filtrada de los vehículos registrados.
    /// </summary>
    /// <param name="matricula">Filtro por matrícula (parcial o completa).</param>
    /// <param name="marca">Filtro por marca del vehículo.</param>
    /// <param name="motor">Filtro por el tipo de motor de combustión/eléctrico.</param>
    /// <param name="matriculacionDesde">Fecha inicial del rango de matriculación.</param>
    /// <param name="matriculacionHasta">Fecha final del rango de matriculación.</param>
    /// <param name="pagina">Número de la página actual (basado en 1).</param>
    /// <param name="tamanoPagina">Cantidad de registros por página.</param>
    /// <param name="incluirEliminados">Indica si se deben listar vehículos con borrado lógico.</param>
    /// <returns>Colección diferida de los vehículos que cumplen con los criterios.</returns>
    IEnumerable<Vehiculo> ObtenerFiltrados(
        string? matricula, 
        string? marca, 
        TipoMotor? motor, 
        DateTime? matriculacionDesde, 
        DateTime? matriculacionHasta, 
        int pagina = 1, 
        int tamanoPagina = 10, 
        bool incluirEliminados = false);

    /// <summary>
    ///     Busca un vehículo específico por su identificador único numérico.
    /// </summary>
    /// <param name="id">Identificador único del vehículo.</param>
    /// <returns>La instancia del <see cref="Vehiculo"/> si se encuentra; de lo contrario, <c>null</c>.</returns>
    Vehiculo? ObtenerPorId(int id);
    
    /// <summary>
    ///     Busca un vehículo en el sistema utilizando su matrícula legal.
    /// </summary>
    /// <param name="matricula">Matrícula a consultar.</param>
    /// <returns>El <see cref="Vehiculo"/> asociado o <c>null</c> si no existe.</returns>
    Vehiculo? ObtenerPorMatricula(string matricula);

    /// <summary>
    ///     Registra un nuevo vehículo en el sistema tras validar sus reglas e integridad.
    /// </summary>
    /// <param name="vehiculo">Datos del vehículo a registrar.</param>
    /// <returns>Un <see cref="Result{T, TError}"/> con el vehículo persistido o el fallo de dominio correspondiente.</returns>
    Result<Vehiculo, DomainError> Registrar(Vehiculo vehiculo);

    /// <summary>
    ///     Actualiza los datos de un vehículo existente controlando la colisión de matrículas.
    /// </summary>
    /// <param name="id">Identificador del vehículo que se desea modificar.</param>
    /// <param name="vehiculo">Nuevos datos a aplicar.</param>
    /// <returns>Un <see cref="Result{T, TError}"/> con el estado actualizado o un error si no es válido.</returns>
    Result<Vehiculo, DomainError> Actualizar(int id, Vehiculo vehiculo);

    /// <summary>
    ///     Elimina un vehículo del sistema, permitiendo escoger entre borrado físico o lógico.
    /// </summary>
    /// <param name="id">Identificador del vehículo a eliminar.</param>
    /// <param name="borradoLogico"><c>true</c> para realizar una baja lógica (<c>IsDeleted = true</c>); <c>false</c> para purgar de la base de datos.</param>
    /// <returns><c>true</c> si el vehículo fue eliminado con éxito; de lo contrario, <c>false</c>.</returns>
    bool Eliminar(int id, bool borradoLogico = true);

    /// <summary>
    ///     Revierte el estado de borrado lógico de un vehículo restaurándolo en el sistema.
    /// </summary>
    /// <param name="id">Identificador del vehículo a recuperar.</param>
    /// <returns>Resultado con el vehículo restaurado o un error si no se pudo procesar.</returns>
    Result<Vehiculo, DomainError> Restaurar(int id);
    
    /// <summary>
    ///     Calcula el número total de vehículos bajo los criterios de eliminación especificados.
    /// </summary>
    /// <param name="incluirEliminados"><c>true</c> para contar también los registros con baja lógica.</param>
    /// <returns>Cantidad total de registros de vehículos.</returns>
    int Contar(bool incluirEliminados = false);
}