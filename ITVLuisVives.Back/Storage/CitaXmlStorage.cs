using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using CSharpFunctionalExtensions;
using ITVLuisVives.Back.Dto;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Mappers;
using ITVLuisVives.Back.Models;
using Serilog;

namespace ITVLuisVives.Back.Storage;

public class CitaXmlStorage : ICitaXmlStorage
{
    private readonly ILogger _logger = Log.ForContext<CitaXmlStorage>();
    private readonly XmlSerializer _serializer = new(typeof(List<CitaDto>));

    public CitaXmlStorage() => InitStorage();

    public Result<bool, DomainError> Salvar(IEnumerable<Cita> items, string path)
    {
        try
        {
            _logger.Debug("Guardando citas en XML en '{path}'", path);
            var dtos = items.Select(c => c.ToDto()).ToList();
            using var writer = new StreamWriter(path, false, Encoding.UTF8);
            _serializer.Serialize(writer, dtos);
            return Result.Success<bool, DomainError>(true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al guardar XML en '{path}'", path);
            return Result.Failure<bool, DomainError>(new DomainError(ex.Message, "STORAGE_WRITE_ERROR"));
        }
    }

    public Result<IEnumerable<Cita>, DomainError> Cargar(string path)
    {
        if (!File.Exists(path))
            return Result.Failure<IEnumerable<Cita>, DomainError>(new DomainError($"No encontrado: {path}", "FILE_NOT_FOUND"));

        try
        {
            using var reader = new StreamReader(path, Encoding.UTF8);
            var dtos = (List<CitaDto>?)_serializer.Deserialize(reader);
            return Result.Success<IEnumerable<Cita>, DomainError>(dtos?.Select(d => d.ToModel()).ToList() ?? new List<Cita>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al cargar XML de '{path}'", path);
            return Result.Failure<IEnumerable<Cita>, DomainError>(new DomainError(ex.Message, "STORAGE_READ_ERROR"));
        }
    }

    private void InitStorage()
    {
        if (!Directory.Exists("data")) Directory.CreateDirectory("data");
    }
}