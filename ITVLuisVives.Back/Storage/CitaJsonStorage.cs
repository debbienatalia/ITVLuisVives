using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CSharpFunctionalExtensions;
using ITVLuisVives.Back.Dto;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Mappers;
using ITVLuisVives.Back.Models;
using Serilog;

namespace ITVLuisVives.Back.Storage;

public class CitaJsonStorage : ICitaJsonStorage
{
    private readonly ILogger _logger = Log.ForContext<CitaJsonStorage>();
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public CitaJsonStorage() => InitStorage();

    public Result<bool, DomainError> Salvar(IEnumerable<Cita> items, string path)
    {
        try
        {
            _logger.Debug("Guardando citas en JSON en '{path}'", path);
            var dtos = items.Select(c => c.ToDto()).ToList();
            var json = JsonSerializer.Serialize(dtos, _options);
            File.WriteAllText(path, json, new UTF8Encoding(false));
            return Result.Success<bool, DomainError>(true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al guardar citas en '{path}'", path);
            return Result.Failure<bool, DomainError>(new DomainError(ex.Message, "STORAGE_WRITE_ERROR"));
        }
    }

    public Result<IEnumerable<Cita>, DomainError> Cargar(string path)
    {
        if (!File.Exists(path))
            return Result.Failure<IEnumerable<Cita>, DomainError>(new DomainError($"No encontrado: {path}", "FILE_NOT_FOUND"));

        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            var dtos = JsonSerializer.Deserialize<List<CitaDto>>(json, _options);
            return Result.Success<IEnumerable<Cita>, DomainError>(dtos?.Select(d => d.ToModel()).ToList() ?? new List<Cita>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al cargar citas de '{path}'", path);
            return Result.Failure<IEnumerable<Cita>, DomainError>(new DomainError(ex.Message, "STORAGE_READ_ERROR"));
        }
    }

    private void InitStorage()
    {
        if (!Directory.Exists("data")) Directory.CreateDirectory("data");
    }
}