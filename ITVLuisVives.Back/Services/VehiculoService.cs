using System;
using System.Collections.Generic;
using CSharpFunctionalExtensions;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Repositories;
using ITVLuisVives.Back.Validators;

namespace ITVLuisVives.Back.Services.Vehiculos;

/// <summary>
///     Implementación de los servicios de negocio del vehículo.
///     Coordina las operaciones entre la persistencia de datos y las restricciones del dominio.
/// </summary>
public class VehiculoService : IVehiculoService
{
    private readonly IVehiculoRepository _repository;
    private readonly IValidator<Vehiculo> _validator;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="VehiculoService"/>.
    /// </summary>
    /// <param name="repository">Instancia inyectada del repositorio de datos.</param>
    /// <param name="validator">Instancia inyectada del validador de dominio.</param>
    public VehiculoService(IVehiculoRepository repository, IValidator<Vehiculo> validator)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <inheritdoc />
    public IEnumerable<Vehiculo> ObtenerFiltrados(
        string? matricula, 
        string? marca, 
        TipoMotor? motor, 
        DateTime? matriculacionDesde, 
        DateTime? matriculacionHasta, 
        int pagina = 1, 
        int tamanoPagina = 10, 
        bool incluirEliminados = false)
    {
        string? matriculaLimpia = matricula?.Trim().ToUpper();

        return _repository.GetFiltered(
            matriculaLimpia, 
            marca, 
            motor, 
            matriculacionDesde, 
            matriculacionHasta, 
            pagina, 
            tamanoPagina, 
            incluirEliminados);
    }

    /// <inheritdoc />
    public Vehiculo? ObtenerPorId(int id)
    {
        return _repository.GetById(id);
    }

    /// <inheritdoc />
    public Vehiculo? ObtenerPorMatricula(string matricula)
    {
        if (string.IsNullOrWhiteSpace(matricula)) return null;
        return _repository.GetByMatricula(matricula.Trim().ToUpper());
    }

    /// <inheritdoc />
    public Result<Vehiculo, DomainError> Registrar(Vehiculo vehiculo)
    {
        var validacion = _validator.Validar(vehiculo);
        if (validacion.IsFailure) return validacion;

        string matriculaFormateada = vehiculo.Matricula.Trim().ToUpper();

        if (_repository.ExisteMatricula(matriculaFormateada))
        {
            return Result.Failure<Vehiculo, DomainError>(
                new DomainError($"Ya existe un vehículo registrado con la matrícula {matriculaFormateada}.", "DUPLICATE_MATRICULA"));
        }

        var vehiculoAInsertar = vehiculo with { Matricula = matriculaFormateada };

        return _repository.Create(vehiculoAInsertar);
    }

    /// <inheritdoc />
    public Result<Vehiculo, DomainError> Actualizar(int id, Vehiculo vehiculo)
    {
        var existente = _repository.GetById(id);
        if (existente == null)
            return Result.Failure<Vehiculo, DomainError>(new DomainError("El vehículo solicitado no existe o ha sido eliminado del sistema.", "NOT_FOUND"));

        var validacion = _validator.Validar(vehiculo);
        if (validacion.IsFailure) return validacion;

        string nuevaMatricula = vehiculo.Matricula.Trim().ToUpper();

        if (!existente.Matricula.Equals(nuevaMatricula, StringComparison.OrdinalIgnoreCase))
        {
            if (_repository.ExisteMatricula(nuevaMatricula))
            {
                return Result.Failure<Vehiculo, DomainError>(
                    new DomainError($"La matrícula {nuevaMatricula} ya pertenece a otro vehículo.", "DUPLICATE_MATRICULA"));
            }
        }

        var vehiculoAActualizar = vehiculo with { Matricula = nuevaMatricula };
        return _repository.Update(id, vehiculoAActualizar);
    }

    /// <inheritdoc />
    public bool Eliminar(int id, bool borradoLogico = true)
    {
        var vehiculoEliminado = _repository.Delete(id, borradoLogico);
        return vehiculoEliminado != null;
    }

    /// <inheritdoc />
    public Result<Vehiculo, DomainError> Restaurar(int id)
    {
        return _repository.Restore(id);
    }

    /// <inheritdoc />
    public int Contar(bool incluirEliminados = false)
    {
        return _repository.Count(incluirEliminados);
    }
}