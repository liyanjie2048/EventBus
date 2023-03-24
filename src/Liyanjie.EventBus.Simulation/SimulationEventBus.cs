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
    readonly ISimulationEventQueue _eventQueue;
    readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="subscriptionsManager"></param>
    /// <param name="eventQueue"></param>
    /// <param name="serviceProvider"></param>
    public SimulationEventBus(
        ILogger<SimulationEventBus> logger,
        ISubscriptionsManager subscriptionsManager,
        ISimulationEventQueue eventQueue,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _subscriptionsManager = subscriptionsManager ?? new InMemorySubscriptionsManager();
        _eventQueue = eventQueue;
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

        _eventQueue.AddChannel();
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
        var result = await _eventQueue.PushAsync(new SimulationEvent
        {
            Name = _subscriptionsManager.GetEventKey<TEvent>(),
            EventData = JsonSerializer.Serialize(eventData),
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

    CancellationTokenSource? tokenSource;
    Task? task;
    void DoConsume()
    {
        tokenSource = new CancellationTokenSource();
        task = Task.Factory.StartNew(async token =>
        {
            var cancellationToken = (CancellationToken)token;

            while (!cancellationToken.IsCancellationRequested)
            {
                SimulationEvent? result = default;
                try
                {
                    result = await _eventQueue.PopAsync();
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

                _logger.LogInformation($"Consume message '{result?.Name}:{result?.EventData}'.");

                var eventName = result!.Name;
                var eventMessage = result!.EventData;
                await ProcessEventAsync(eventName!, eventMessage!);
            }
        }, tokenSource.Token);
    }
    async Task ProcessEventAsync(string eventName, string eventMessage)
    {
        if (!_subscriptionsManager.HasSubscriptions(eventName))
            return;

        using var scope = _serviceProvider.CreateScope();
        foreach (var (handlerType, eventType) in _subscriptionsManager.GetEventHandlerTypes(eventName))
        {
            try
            {
                var handler = ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, handlerType);
                var handleAsync = handler.GetType().GetMethod(nameof(IEventHandler<object>.HandleAsync));
                await (Task)handleAsync.Invoke(handler, new[] { JsonSerializer.Deserialize(eventMessage, eventType) });
                _logger.LogTrace($"{handlerType.FullName}=>{eventMessage}");
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
            _eventQueue.RemoveChannel();
            task?.Dispose();
            task = null;
        }
    }
}
