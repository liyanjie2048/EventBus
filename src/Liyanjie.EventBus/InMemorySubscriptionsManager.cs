using System;
using System.Collections.Generic;
using System.Linq;

namespace Liyanjie.EventBus
{
    /// <summary>
    /// 
    /// </summary>
    public class InMemorySubscriptionsManager : ISubscriptionsManager
    {
        readonly Dictionary<string, Type> eventTypes = new Dictionary<string, Type>();
        readonly Dictionary<string, List<Type>> eventHandlers = new Dictionary<string, List<Type>>();

        /// <summary>
        /// 
        /// </summary>
        public bool IsEmpty => !eventHandlers.Keys.Any();

        /// <summary>
        /// 
        /// </summary>
        public void Clear() => eventHandlers.Clear();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <typeparam name="TEventHandler"></typeparam>
        public void AddSubscription<TEvent, TEventHandler>()
            where TEventHandler : IEventHandler<TEvent>
        {
            var eventName = GetEventKey<TEvent>();
            eventTypes[eventName] = typeof(TEvent);

            var handlerType = typeof(TEventHandler);
            if (!HasSubscriptions(eventName))
                eventHandlers.Add(eventName, new List<Type>());

            if (eventHandlers[eventName].Any(_ => _ == handlerType))
                throw new ArgumentException($"Handler type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));

            eventHandlers[eventName].Add(handlerType);
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
            var eventHandler = eventHandlers[eventName].SingleOrDefault(_ => _ == handlerType);
            if (eventHandler == null)
                return;

            eventHandlers[eventName].Remove(eventHandler);
            if (!eventHandlers[eventName].Any())
            {
                eventHandlers.Remove(eventName);
                eventTypes.Remove(eventName);

                OnEventRemoved.Invoke(this, eventName);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public bool HasSubscriptions(string eventName) => eventHandlers.ContainsKey(eventName);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetEventNames() => eventTypes.Keys;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public IEnumerable<Type> GetEventHandlerTypes(string eventName) => eventHandlers[eventName];

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public Type GetEventType(string eventName) => eventTypes.TryGetValue(eventName, out var type) ? type : null;

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
        public event EventHandler<string> OnEventRemoved;
    }
}
