using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CSharpFunctionalExtensions;
using ITVLuisVives.Back.Dto;
using ITVLuisVives.Back.Errors;
using ITVLuisVives.Back.Mappers;
using ITVLuisVives.Back.Models;
using Serilog;

namespace ITVLuisVives.Back.Storage;

public class CitaCsvStorage : ICitaCsvStorage
{
    private readonly ILogger _logger = Log.ForContext<CitaCsvStorage>();

    public CitaCsvStorage() => InitStorage();

    public Result<bool, DomainError> Salvar(IEnumerable<Cita> items, string path)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("Id;Dni;VehiculoMatricula;FechaInspeccion;Estado;Observaciones;EsHoy;CreatedAt;UpdatedAt;IsDeleted;DeletedAt");

            foreach (var item in items)
            {
                var d = item.ToDto();
                sb.AppendLine($"{d.Id};{d.Dni};{d.VehiculoMatricula};{d.FechaInspeccion};{d.Estado};{d.Observaciones};{d.EsHoy};{d.CreatedAt};{d.UpdatedAt};{d.IsDeleted};{d.DeletedAt}");
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return Result.Success<bool, DomainError>(true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al escribir CSV en '{path}'", path);
            return Result.Failure<bool, DomainError>(new DomainError(ex.Message, "STORAGE_WRITE_ERROR"));
        }
    }

    public Result<IEnumerable<Cita>, DomainError> Cargar(string path)
    {
        if (!File.Exists(path))
            return Result.Failure<IEnumerable<Cita>, DomainError>(new DomainError($"No encontrado: {path}", "FILE_NOT_FOUND"));

        try
        {
            var citas = File.ReadLines(path, Encoding.UTF8)
                .Skip(1)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(line => line.Split(';'))
                .Select(col => new CitaDto(
                    col[0], col[1], col[2], col[3], col[4], col[5],
                    bool.Parse(col[6]), col[7], col[8], bool.Parse(col[9]), 
                    string.IsNullOrEmpty(col[10]) ? null : col[10]
                ).ToModel());

            return Result.Success<IEnumerable<Cita>, DomainError>(citas.ToList());
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al cargar CSV en '{path}'", path);
            return Result.Failure<IEnumerable<Cita>, DomainError>(new DomainError(ex.Message, "INVALID_FORMAT"));
        }
    }

    private void InitStorage()
    {
        if (!Directory.Exists("data")) Directory.CreateDirectory("data");
    }
}