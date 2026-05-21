using System;
using System.Collections.Generic;
using CSharpFunctionalExtensions;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Enums;

namespace ITVLuisVives.Back.Repositories;

public interface ICitaRepository 
{
    // Buscador de citas por cualquier campo y rangos temporales
    IEnumerable<Cita> GetFiltered(
        string? dni,
        string? matricula,
        EstadoCita? estado,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int page = 1,
        int pageSize = 10,
        bool includeDeleted = false);

    Cita? GetById(Guid id);

    // Regla de negocio: Un vehículo no puede repetir inspección el mismo día
    bool ExisteCitaParaVehiculoEnFecha(string matricula, DateTime fecha);

    // Regla de negocio: Un DNI no puede tener más de 3 citas en la misma fecha
    int CountCitasPorDniYFecha(string dni, DateTime fecha);

    Result<Cita, DomainError> Create(Cita cita);
    
    Result<Cita, DomainError> Update(Guid id, Cita cita);

    Cita? Delete(Guid id, bool isLogical = true);

    Result<Cita, DomainError> Restore(Guid id);

    int Count(bool includeDeleted = false);
}