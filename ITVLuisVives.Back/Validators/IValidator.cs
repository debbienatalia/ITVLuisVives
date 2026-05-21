using CSharpFunctionalExtensions;
using ITVLuisVives.Back.Errors;

namespace ITVLuisVives.Back.Validators;

/// <summary>
///     Contrato para validar entidades del dominio.
/// </summary>
/// <typeparam name="T">Tipo de entidad a validar.</typeparam>
public interface IValidator<T>
{
    /// <summary>
    ///     Valida una entidad según las reglas de dominio.
    /// </summary>
    /// <param name="entidad">Entidad a validar.</param>
    /// <returns>
    ///     Result con la entidad validada o un error <see cref="DomainError" /> si la estructura o los datos no son válidos.
    /// </returns>
    Result<T, DomainError> Validar(T entidad);
}