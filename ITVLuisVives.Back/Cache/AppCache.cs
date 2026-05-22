using System;
using System.Collections.Generic;
using Serilog;

namespace ITVLuisVives.Back.Cache;

/// <summary>
///     Almacenamiento en caché de alta disponibilidad gestionado bajo la política LRU (Least Recently Used).
///     Garantiza accesos de lectura y escritura en tiempo constante <c>O(1)</c> mediante un mapa indexado,
///     asociado a un registro cronológico de uso doblemente enlazado.
/// </summary>
public class AppCache<TKey, TValue> : ICache<TKey, TValue> where TKey : notnull {
    private readonly int _capacityLimit;
    private readonly Dictionary<TKey, TValue> _cacheStore = new();
    private readonly LinkedList<TKey> _nodeTracker = new();
    private readonly ILogger _logger = Log.ForContext<AppCache<TKey, TValue>>();

    public AppCache(int capacityLimit) {
        if (capacityLimit <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacityLimit), "La capacidad de la caché debe ser mayor que cero.");
        
        _capacityLimit = capacityLimit;
    }

    /// <inheritdoc cref="ICache{TKey,TValue}.Add" />
    public void Add(TKey key, TValue value) {
        _logger.Debug("[Cache:Add] Intentando registrar clave: {Key}", key);

        if (_cacheStore.TryGetValue(key, out var oldVal)) {
            _logger.Debug("[Cache:Update] Clave existente detectada ({Key}). Actualizando valor: {Old} -> {New}", key, oldVal, value);
            _cacheStore[key] = value;
            PromoteKey(key);
            return;
        }

        _logger.Debug("[Cache:Add] Evaluando nueva entrada para la clave: {Key}. Ocupación actual: {Count}/{Limit}", key, _cacheStore.Count, _capacityLimit);

        if (_cacheStore.Count >= _capacityLimit) {
            ExecuteEviction();
        }

        _cacheStore.Add(key, value);
        _nodeTracker.AddLast(key);
        _logger.Debug("[Cache:Success] Entrada añadida. Orden de uso actual: {Order}", string.Join(" -> ", _nodeTracker));
    }

    /// <inheritdoc cref="ICache{TKey,TValue}.Get" />
    public TValue? Get(TKey key) {
        _logger.Debug("[Cache:Get] Solicitando clave: {Key}", key);

        if (!_cacheStore.TryGetValue(key, out var value)) {
            _logger.Debug("[Cache:Miss] La clave {Key} no se encuentra en el almacén", key);
            return default;
        }

        _logger.Debug("[Cache:Hit] Clave {Key} localizada ({Value}). Reordenando prioridad (MRU)...", key, value);
        PromoteKey(key);
        _logger.Debug("[Cache:Get] Estado del historial tras consulta: {Order}", string.Join(" -> ", _nodeTracker));

        return value;
    }

    /// <inheritdoc cref="ICache{TKey,TValue}.Remove" />
    public bool Remove(TKey key) {
        _logger.Debug("[Cache:Remove] Solicitando remoción manual de la clave: {Key}", key);

        if (!_cacheStore.Remove(key)) {
            _logger.Debug("[Cache:Remove] No se pudo eliminar. La clave {Key} no existía", key);
            return false;
        }

        _nodeTracker.Remove(key);
        _logger.Information("[Cache:Remove] Clave {Key} eliminada con éxito del almacén e historial", key);
        return true;
    }

    /// <inheritdoc cref="ICache{TKey,TValue}.DisplayStatus" />
    public void DisplayStatus() {
        _logger.Information("[Cache:Status] Espacio en uso: {Current}/{Max}", _cacheStore.Count, _capacityLimit);
        _logger.Information("[Cache:Timeline] Historial LRU (Antiguo -> Reciente): {History}", string.Join(" -> ", _nodeTracker));
    }

    /// <summary>
    ///     Mueve una clave activa al extremo final de la lista, marcándola como el elemento usado más recientemente.
    /// </summary>
    private void PromoteKey(TKey key) {
        _logger.Verbose("[Cache:Refresh] Promocionando clave {Key} al final de la cola de uso", key);
        _nodeTracker.Remove(key);
        _nodeTracker.AddLast(key);
    }

    /// <summary>
    ///     Ejecuta el desalojo del elemento más antiguo de la caché al haber alcanzado el umbral máximo de capacidad.
    /// </summary>
    private void ExecuteEviction() {
        var victimKey = _nodeTracker.First!.Value;
        var victimValue = _cacheStore[victimKey];

        _logger.Information("[Cache:Evict] Límite alcanzado. Desalojando elemento menos utilizado: {Key} = {Value}", victimKey, victimValue);
        
        _nodeTracker.RemoveFirst();
        _cacheStore.Remove(victimKey);
    }
}