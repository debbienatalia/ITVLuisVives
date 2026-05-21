using System;
using System.Collections.Generic;
using CSharpFunctionalExtensions;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Enums;

namespace ITVLuisVives.Back.Repositories;

public interface IVehiculoRepository 
{
    // Buscador avanzado multi-parámetro
    IEnumerable<Vehiculo> GetFiltered(
        string? matricula, 
        string? marca, 
        TipoMotor? motor, 
        DateTime? matriculacionDesde, 
        DateTime? matriculacionHasta, 
        int page = 1, 
        int pageSize = 10, 
        bool includeDeleted = false);

    Vehiculo? GetById(int id);
    
    Vehiculo? GetByMatricula(string matricula);
    
    bool ExisteMatricula(string matricula);

    Result<Vehiculo, DomainError> Create(Vehiculo vehiculo);
    
    Result<Vehiculo, DomainError> Update(int id, Vehiculo vehiculo);

    // Borrado configurable (físico o lógico con flag)
    Vehiculo? Delete(int id, bool isLogical = true);

    // Permite revertir un borrado lógico
    Result<Vehiculo, DomainError> Restore(int id);
    
    int Count(bool includeDeleted = false);
}