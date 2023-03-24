using System;
using System.Collections.Generic;
using System.Linq;

namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class InMemorySubscriptionsManager : ISubscriptionsManager
{
    readonly Dictionary<string, List<(Type HandlerType, Type EventType)>> _handlers = new();

    /// <summary>
    /// 
    /// </summary>
    public bool IsEmpty => !_handlers.Keys.Any();

    /// <summary>
    /// 
    /// </summary>
    public void Clear() => _handlers.Clear();

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <typeparam name="TEventHandler"></typeparam>
    public void AddSubscription<TEvent, TEventHandler>()
        where TEventHandler : IEventHandler<TEvent>
    {
        var eventName = GetEventName<TEvent>();

        var handlerType = typeof(TEventHandler);
        var eventType = typeof(TEvent);
        if (!HasSubscriptionsForEvent(eventName))
            _handlers.Add(eventName, new());

        if (_handlers[eventName].Any(_ => _.HandlerType == handlerType))
            throw new ArgumentException($"Handler type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));

        _handlers[eventName].Add((handlerType, eventType));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <typeparam name="TEventHandler"></typeparam>
    public void RemoveSubscription<TEvent, TEventHandler>()
        where TEventHandler : IEventHandler<TEvent>
    {
        var eventName = GetEventName<TEvent>();
        if (!HasSubscriptionsForEvent(eventName))
            return;

        var handlerType = typeof(TEventHandler);
        var eventHandler = _handlers[eventName].SingleOrDefault(_ => _.HandlerType == handlerType);
        if (eventHandler == default)
            return;

        _handlers[eventName].Remove(eventHandler);
        if (!_handlers[eventName].Any())
        {
            _handlers.Remove(eventName);
            OnEventRemoved?.Invoke(this, eventName);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetEventNames() => _handlers.Keys;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    public IEnumerable<(Type HandlerType, Type EventType)> GetEventHandlerTypes(string eventName) => _handlers[eventName];

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    public string GetEventName<TEvent>() => typeof(TEvent).Name;

    /// <summary>
    /// 
    /// </summary>
    public event EventHandler<string>? OnEventRemoved;
}
