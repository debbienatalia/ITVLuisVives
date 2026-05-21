using System;
using System.Collections.Generic;
using CSharpFunctionalExtensions;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Repositories;
using ITVLuisVives.Back.Validators;

namespace ITVLuisVives.Back.Services.Citas;

/// <summary>
///     Implementación de los servicios de negocio de la Cita.
///     Garantiza el cumplimiento de los límites de cupo por usuario y la unicidad de inspecciones por vehículo.
/// </summary>
public class CitaService : ICitaService
{
    private readonly ICitaRepository _repository;
    private readonly IValidator<Cita> _validator;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="CitaService"/>.
    /// </summary>
    /// <param name="repository">Instancia inyectada del repositorio de citas.</param>
    /// <param name="validator">Instancia inyectada del validador de dominio de citas.</param>
    public CitaService(ICitaRepository repository, IValidator<Cita> validator)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <inheritdoc />
    public IEnumerable<Cita> ObtenerFiltradas(
        string? dni,
        string? matricula,
        EstadoCita? estado,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int pagina = 1,
        int tamanoPagina = 10,
        bool incluirEliminados = false)
    {
        string? dniLimpio = dni?.Trim().ToUpper().Replace(" ", "").Replace("-", "");
        string? matriculaLimpia = matricula?.Trim().ToUpper().Replace(" ", "").Replace("-", "");

        return _repository.GetFiltered(
            dniLimpio,
            matriculaLimpia,
            estado,
            fechaDesde,
            fechaHasta,
            pagina,
            tamanoPagina,
            incluirEliminados);
    }

    /// <inheritdoc />
    public Cita? ObtenerPorId(Guid id)
    {
        return _repository.GetById(id);
    }

    /// <inheritdoc />
    public Result<Cita, DomainError> Agendar(Cita cita)
    {
        var validacion = _validator.Validar(cita);
        if (validacion.IsFailure) return validacion;

        string dniFormateado = cita.Dni.Trim().ToUpper().Replace(" ", "").Replace("-", "");
        string matriculaFormateada = cita.VehiculoMatricula.Trim().ToUpper().Replace(" ", "").Replace("-", "");
        DateTime fechaDestino = cita.FechaInspeccion.Date;

        if (_repository.ExisteCitaParaVehiculoEnFecha(matriculaFormateada, fechaDestino))
        {
            return Result.Failure<Cita, DomainError>(
                new DomainError($"El vehículo con matrícula {matriculaFormateada} ya tiene una cita agendada para el {fechaDestino:dd/MM/yyyy}.", "VEHICLE_ALREADY_BOOKED"));
        }

        if (_repository.CountCitasPorDniYFecha(dniFormateado, fechaDestino) >= 3)
        {
            return Result.Failure<Cita, DomainError>(
                new DomainError($"El DNI {dniFormateado} ha alcanzado el cupo máximo de 3 citas para el día {fechaDestino:dd/MM/yyyy}.", "MAX_DNI_LIMIT_EXCEEDED"));
        }

        var citaAInsertar = cita with 
        { 
            Dni = dniFormateado, 
            VehiculoMatricula = matriculaFormateada 
        };

        return _repository.Create(citaAInsertar);
    }

    /// <inheritdoc />
    public Result<Cita, DomainError> Actualizar(Guid id, Cita cita)
    {
        var existente = _repository.GetById(id);
        if (existente == null)
            return Result.Failure<Cita, DomainError>(new DomainError("La cita que intenta modificar no existe o ha sido eliminada.", "NOT_FOUND"));

        var validacion = _validator.Validar(cita);
        if (validacion.IsFailure) return validacion;

        string nuevoDni = cita.Dni.Trim().ToUpper().Replace(" ", "").Replace("-", "");
        string nuevaMatricula = cita.VehiculoMatricula.Trim().ToUpper().Replace(" ", "").Replace("-", "");
        DateTime nuevaFecha = cita.FechaInspeccion.Date;

        if (existente.VehiculoMatricula != nuevaMatricula || existente.FechaInspeccion.Date != nuevaFecha)
        {
            if (_repository.ExisteCitaParaVehiculoEnFecha(nuevaMatricula, nuevaFecha))
            {
                return Result.Failure<Cita, DomainError>(
                    new DomainError($"No se puede reprogramar: el vehículo {nuevaMatricula} ya posee otra cita asignada para el {nuevaFecha:dd/MM/yyyy}.", "VEHICLE_ALREADY_BOOKED"));
            }
        }

        if (existente.Dni != nuevoDni || existente.FechaInspeccion.Date != nuevaFecha)
        {
            if (_repository.CountCitasPorDniYFecha(nuevoDni, nuevaFecha) >= 3)
            {
                return Result.Failure<Cita, DomainError>(
                    new DomainError($"No se puede asignar la cita: el DNI {nuevoDni} ya cubre el límite de 3 registros para el {nuevaFecha:dd/MM/yyyy}.", "MAX_DNI_LIMIT_EXCEEDED"));
            }
        }

        var citaAActualizar = cita with 
        { 
            Dni = nuevoDni, 
            VehiculoMatricula = nuevaMatricula,
            UpdatedAt = DateTime.Now
        };

        return _repository.Update(id, citaAActualizar);
    }

    /// <inheritdoc />
    public bool Cancelar(Guid id, bool borradoLogico = true)
    {
        var citaCancelada = _repository.Delete(id, borradoLogico);
        return citaCancelada != null;
    }

    /// <inheritdoc />
    public Result<Cita, DomainError> Restaurar(Guid id)
    {
        return _repository.Restore(id);
    }

    /// <inheritdoc />
    public int Contar(bool incluirEliminados = false)
    {
        return _repository.Count(incluirEliminados);
    }
}