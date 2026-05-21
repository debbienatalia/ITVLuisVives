namespace ITVLuisVives.Back.Errors;

/// <summary>
///     Representa un error específico de la lógica de negocio o del dominio.
/// </summary>
public record DomainError(string Message, string? Code = null)
{
    public static DomainError NotFound(string entityName, string identifier) 
        => new($"No se encontró la entidad '{entityName}' con el identificador: {identifier}.", "NOT_FOUND");

    public static DomainError AlreadyExists(string message) 
        => new(message, "ALREADY_EXISTS");
}