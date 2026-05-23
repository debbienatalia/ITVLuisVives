using System;
using System.Collections.Generic;
using CSharpFunctionalExtensions;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Enums;

namespace ITVLuisVives.Back.Services;

/// <summary>
///     Define el contrato de negocio para el control, reserva y gestión de citas de la ITV.
/// </summary>
public interface ICitaService
{
    /// <summary>
    ///     Obtiene una lista paginada y filtrada de las citas programadas en el sistema.
    /// </summary>
    /// <param name="dni">Filtro por el DNI del propietario.</param>
    /// <param name="matricula">Filtro por la matrícula del vehículo.</param>
    /// <param name="estado">Filtro por el estado de la cita (<see cref="EstadoCita"/>).</param>
    /// <param name="fechaDesde">Fecha inicial del rango de inspección.</param>
    /// <param name="fechaHasta">Fecha final del rango de inspección.</param>
    /// <param name="pagina">Número de la página actual (basado en 1).</param>
    /// <param name="tamanoPagina">Cantidad de registros por página.</param>
    /// <param name="incluirEliminados">Indica si se deben listar citas canceladas mediante borrado lógico.</param>
    /// <returns>Colección diferida de las citas que cumplen los criterios de búsqueda.</returns>
    IEnumerable<Cita> ObtenerFiltradas(
        string? dni,
        string? matricula,
        EstadoCita? estado,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int pagina = 1,
        int tamanoPagina = 10,
        bool incluirEliminados = false);

    /// <summary>
    ///     Busca una cita específica utilizando su identificador único global (Guid).
    /// </summary>
    /// <param name="id">Identificador único de la cita.</param>
    /// <returns>La instancia de la <see cref="Cita"/> si se encuentra; de lo contrario, <c>null</c>.</returns>
    Cita? ObtenerPorId(Guid id);

    /// <summary>
    ///     Agenda una nueva cita en el sistema evaluando las restricciones de ocupación y calendario.
    /// </summary>
    /// <param name="cita">Datos de la cita que se desea programar.</param>
    /// <returns>Un <see cref="Result{T, TError}"/> con la cita persistida o el fallo de dominio correspondiente.</returns>
    Result<Cita, DomainError> Agendar(Cita cita);

    /// <summary>
    ///     Modifica los parámetros de una cita existente (reprogramación u observaciones).
    /// </summary>
    /// <param name="id">Identificador de la cita que se desea modificar.</param>
    /// <param name="cita">Nuevos datos a aplicar.</param>
    /// <returns>Un <see cref="Result{T, TError}"/> con el estado actualizado o un error si infringe alguna regla.</returns>
    Result<Cita, DomainError> Actualizar(Guid id, Cita cita);

    /// <summary>
    ///     Cancela una cita del sistema, permitiendo escoger entre borrado físico o lógico.
    /// </summary>
    /// <param name="id">Identificador de la cita a cancelar.</param>
    /// <param name="borradoLogico"><c>true</c> para realizar una baja lógica (<c>IsDeleted = true</c>); <c>false</c> para purgar de la base de datos.</param>
    /// <returns><c>true</c> si la cita fue cancelada con éxito; de lo contrario, <c>false</c>.</returns>
    bool Cancelar(Guid id, bool borradoLogico = true);

    /// <summary>
    ///     Revierte la cancelación de una cita restaurando su vigencia original.
    /// </summary>
    /// <param name="id">Identificador de la cita a recuperar.</param>
    /// <returns>Resultado con la cita restaurada o un error si no se pudo procesar.</returns>
    Result<Cita, DomainError> Restaurar(Guid id);

    /// <summary>
    ///     Calcula el número total de citas bajo los criterios de eliminación especificados.
    /// </summary>
    /// <param name="incluirEliminados"><c>true</c> para contar también las citas canceladas de forma lógica.</param>
    /// <returns>Cantidad total de registros de citas.</returns>
    int Contar(bool incluirEliminados = false);
}