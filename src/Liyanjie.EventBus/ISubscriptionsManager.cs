using System;
using System.Collections.Generic;

namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public interface ISubscriptionsManager
{
    /// <summary>
    /// 
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// 
    /// </summary>
    void Clear();

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <typeparam name="TEventHandler"></typeparam>
    void AddSubscription<TEvent, TEventHandler>()
       where TEventHandler : IEventHandler<TEvent>;

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <typeparam name="TEventHandler"></typeparam>
    void RemoveSubscription<TEvent, TEventHandler>()
         where TEventHandler : IEventHandler<TEvent>;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    bool HasSubscriptions(string eventName);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    string GetEventKey<TEvent>();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerable<string> GetEventNames();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    IEnumerable<(Type HandlerType, Type EventType)> GetEventHandlerTypes(string eventName);

    /// <summary>
    /// 
    /// </summary>
    event EventHandler<string> OnEventRemoved;
}
