namespace ITVLuisVives.Back.Cache;

/// <summary>
/// Contrato para una estructura de caché genérica basada en el algoritmo de desalojo LRU (Least Recently Used).
/// </summary>
/// <typeparam name="TKey">Tipo de la clave (restringida a valores no nulos).</typeparam>
/// <typeparam name="TValue">Tipo del valor almacenado dentro de la caché.</typeparam>
public interface ICache<in TKey, TValue> where TKey : notnull {
    
    /// <summary>
    /// Agrega un elemento a la caché. Si está llena, elimina el menos usado.
    /// </summary>
    void Add(TKey key, TValue value);

    /// <summary>
    /// Obtiene un elemento de la caché.
    /// </summary>
    TValue? Get(TKey key);

    /// <summary>
    /// Elimina un elemento de la caché.
    /// </summary>
    bool Remove(TKey key);

    /// <summary>
    /// Muestra por los logs el estado actual de ocupación de la caché y el orden de prioridad de su historial.
    /// </summary>
    void DisplayStatus();
}