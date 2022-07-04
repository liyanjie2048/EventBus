using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class InMemoryEventBus : IEventBus, IDisposable
{
    readonly ILogger<InMemoryEventBus> _logger;
    readonly ISubscriptionsManager _subscriptionsManager;
    readonly IServiceProvider _serviceProvider;
    readonly ConcurrentQueue<EventWrapper> _eventQueue = new();


    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="subscriptionsManager"></param>
    /// <param name="serviceProvider"></param>
    public InMemoryEventBus(
        ILogger<InMemoryEventBus> logger,
        ISubscriptionsManager subscriptionsManager,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _subscriptionsManager = subscriptionsManager ?? new InMemorySubscriptionsManager();
        _serviceProvider = serviceProvider;
        _subscriptionsManager.OnEventRemoved += SubscriptionsManager_OnEventRemoved;

        DoConsume();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <typeparam name="TEventHandler"></typeparam>
    public void RegisterEventHandler<TEvent, TEventHandler>()
        where TEventHandler : IEventHandler<TEvent>
    {
        _subscriptionsManager.AddSubscription<TEvent, TEventHandler>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <typeparam name="TEventHandler"></typeparam>
    public void RemoveEventHandler<TEvent, TEventHandler>()
        where TEventHandler : IEventHandler<TEvent>
    {
        _subscriptionsManager.RemoveSubscription<TEvent, TEventHandler>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="eventData"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> PublishEventAsync<TEvent>(
        TEvent eventData,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        _eventQueue.Enqueue(new EventWrapper
        {
            Name = _subscriptionsManager.GetEventKey<TEvent>(),
            EventData = JsonSerializer.Serialize(eventData),
        });

        _logger.LogInformation($"Publish event success,list length:{_eventQueue.Count}");

        if (task == null)
            DoConsume();

        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        tokenSource?.Cancel();
        task?.Dispose();
        _subscriptionsManager.Clear();
        _eventQueue.Clear();
    }

    CancellationTokenSource tokenSource;
    Task task;
    void DoConsume()
    {
        tokenSource = new CancellationTokenSource();
        task = Task.Factory.StartNew(async token =>
        {
            var cancellationToken = (CancellationToken)token;

            while (!cancellationToken.IsCancellationRequested)
            {
                var result = _eventQueue.TryDequeue(out var @event) ? @event : null;
                if (result == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    continue;
                }

                _logger.LogInformation($"Consume message '{result.Name}:{result.EventData}'.");

                var eventName = result.Name;
                var eventMessage = result.EventData;
                await ProcessEventAsync(eventName, eventMessage);
            }
        }, tokenSource.Token);
    }
    async Task ProcessEventAsync(string eventName, string eventMessage)
    {
        if (!_subscriptionsManager.HasSubscriptions(eventName))
            return;

        var eventType = _subscriptionsManager.GetEventType(eventName);
        var eventData = JsonSerializer.Deserialize(eventMessage, eventType);
        var handlerMethod = typeof(IEventHandler<>).MakeGenericType(eventType).GetMethod(nameof(IEventHandler<object>.HandleAsync));

        var handlerTypes = _subscriptionsManager.GetEventHandlerTypes(eventName);
        using var scope = _serviceProvider.CreateScope();
        foreach (var handlerType in handlerTypes)
        {
            try
            {
                var handler = ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, handlerType);
                await (Task)handlerMethod.Invoke(handler, new[] { eventData });
                _logger.LogDebug($"{handlerType.FullName}=>{eventMessage}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message} in {handlerType.FullName}=>{eventMessage}");
            }
        }
    }
    void SubscriptionsManager_OnEventRemoved(object sender, string eventName)
    {
        if (_subscriptionsManager.IsEmpty)
        {
            task?.Dispose();
            task = null;
        }
    }

    class EventWrapper
    {
        public string Name { get; set; }
        public string EventData { get; set; }
    }
}
