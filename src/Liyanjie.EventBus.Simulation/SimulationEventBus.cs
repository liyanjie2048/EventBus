using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class SimulationEventBus : IEventBus, IDisposable
{
    readonly ILogger<SimulationEventBus> _logger;
    readonly ISubscriptionsManager _subscriptionsManager;
    readonly ISimulationEventQueue _eventStore;
    readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="subscriptionsManager"></param>
    /// <param name="eventStore"></param>
    /// <param name="serviceProvider"></param>
    public SimulationEventBus(
        ILogger<SimulationEventBus> logger,
        ISubscriptionsManager subscriptionsManager,
        ISimulationEventQueue eventStore,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _subscriptionsManager = subscriptionsManager ?? new InMemorySubscriptionsManager();
        _eventStore = eventStore;
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
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> PublishEventAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
    {
        var result = await _eventStore.PushAsync(new SimulationEvent
        {
            Name = _subscriptionsManager.GetEventKey<TEvent>(),
            EventData = JsonSerializer.Serialize(@event),
        });

        _logger.LogInformation($"Publish event {(result ? "success" : "failed")}");

        if (task == null)
            DoConsume();

        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        tokenSource?.Cancel();
        _subscriptionsManager.Clear();
        task?.Dispose();
        task = null;
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
                SimulationEvent result = default;
                try
                {
                    result = await _eventStore.PopAsync();
                    if (result is null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        continue;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError($"Error occured: {e.Message}");
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
        var @event = JsonSerializer.Deserialize(eventMessage, eventType);
        var handlerMethod = typeof(IEventHandler<>).MakeGenericType(eventType).GetMethod(nameof(IEventHandler<object>.HandleAsync));

        var handlerTypes = _subscriptionsManager.GetEventHandlerTypes(eventName);
        using var scope = _serviceProvider.CreateScope();
        foreach (var handlerType in handlerTypes)
        {
            try
            {
                var handler = ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, handlerType);
                await (Task)handlerMethod.Invoke(handler, new[] { @event });
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
}
