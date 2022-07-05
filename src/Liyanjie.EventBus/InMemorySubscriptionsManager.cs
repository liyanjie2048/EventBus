using System;
using System.Collections.Generic;
using System.Linq;

namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class InMemorySubscriptionsManager : ISubscriptionsManager
{
    readonly Dictionary<string, Type> _eventTypes = new();
    readonly Dictionary<string, List<Type>> _eventHandlers = new();

    /// <summary>
    /// 
    /// </summary>
    public bool IsEmpty => !_eventHandlers.Keys.Any();

    /// <summary>
    /// 
    /// </summary>
    public void Clear() => _eventHandlers.Clear();

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <typeparam name="TEventHandler"></typeparam>
    public void AddSubscription<TEvent, TEventHandler>()
        where TEventHandler : IEventHandler<TEvent>
    {
        var eventName = GetEventKey<TEvent>();
        _eventTypes[eventName] = typeof(TEvent);

        var handlerType = typeof(TEventHandler);
        if (!HasSubscriptions(eventName))
            _eventHandlers.Add(eventName, new List<Type>());

        if (_eventHandlers[eventName].Any(_ => _ == handlerType))
            throw new ArgumentException($"Handler type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));

        _eventHandlers[eventName].Add(handlerType);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <typeparam name="TEventHandler"></typeparam>
    public void RemoveSubscription<TEvent, TEventHandler>()
        where TEventHandler : IEventHandler<TEvent>
    {
        var eventName = GetEventKey<TEvent>();
        if (!HasSubscriptions(eventName))
            return;

        var handlerType = typeof(TEventHandler);
        var eventHandler = _eventHandlers[eventName].SingleOrDefault(_ => _ == handlerType);
        if (eventHandler == null)
            return;

        _eventHandlers[eventName].Remove(eventHandler);
        if (!_eventHandlers[eventName].Any())
        {
            _eventHandlers.Remove(eventName);
            _eventTypes.Remove(eventName);

            OnEventRemoved?.Invoke(this, eventName);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    public bool HasSubscriptions(string eventName) => _eventHandlers.ContainsKey(eventName);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetEventNames() => _eventTypes.Keys;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    public IEnumerable<Type> GetEventHandlerTypes(string eventName) => _eventHandlers[eventName];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    public Type? GetEventType(string eventName)
        => _eventTypes.TryGetValue(eventName, out var type) ? type : null;

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    public string GetEventKey<TEvent>()
    {
        return typeof(TEvent).Name;
    }

    /// <summary>
    /// 
    /// </summary>
    public event EventHandler<string>? OnEventRemoved;
}
