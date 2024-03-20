namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class RedisEventBus : IEventBus, IDisposable
{
    readonly ILogger<RedisEventBus> _logger;
    readonly RedisSettings _settings;
    readonly ISubscriptionsManager _subscriptionsManager;
    readonly IServiceProvider _serviceProvider;
    readonly ConnectionMultiplexer _redis;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="options"></param>
    /// <param name="subscriptionsManager"></param>
    /// <param name="serviceProvider"></param>
    public RedisEventBus(
        ILogger<RedisEventBus> logger,
        IOptions<RedisSettings> options,
        ISubscriptionsManager subscriptionsManager,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _settings = options.Value;
        _subscriptionsManager = subscriptionsManager ?? new InMemorySubscriptionsManager();
        _serviceProvider = serviceProvider;
        _redis = ConnectionMultiplexer.Connect(_settings.ConnectionString ?? throw new ArgumentNullException(nameof(_settings.ConnectionString)));
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

        if (!_redis.GetDatabase().ListRange(_settings.ListKey_Keys).Contains(_settings.ListKey_Channel))
            _redis.GetDatabase().ListRightPush(_settings.ListKey_Keys, _settings.ListKey_Channel);
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
    /// <returns></returns>
    public async Task<bool> PublishEventAsync<TEvent>(TEvent eventData)
    {
        var keys = await _redis.GetDatabase().ListRangeAsync(_settings.ListKey_Keys);
        foreach (var item in keys)
        {
            var length = await _redis.GetDatabase().ListRightPushAsync((string)item!,
                JsonSerializer.Serialize(new EventWrapper
                {
                    Name = _subscriptionsManager.GetEventName<TEvent>(),
                    Message = JsonSerializer.Serialize(eventData),
                }));

            _logger.LogInformation($"Publish event success,list length:{length}");
        }

        if (task == null)
            DoConsume();

        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        _subscriptionsManager.Clear();
        tokenSource?.Cancel();
        task?.Dispose();
        task = null;
    }

    CancellationTokenSource? tokenSource;
    Task? task;
    void DoConsume()
    {
        tokenSource = new CancellationTokenSource();
        task = Task.Factory.StartNew(async () =>
        {
            while (!tokenSource.IsCancellationRequested)
            {
                EventWrapper? result = default;
                try
                {

                    var value = (string)(await _redis.GetDatabase().ListLeftPopAsync(_settings.ListKey_Channel))!;
                    if (value is null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        continue;
                    }

                    result = JsonSerializer.Deserialize<EventWrapper>(value);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Error occured: {e.Message}");
                    continue;
                }

                _logger.LogInformation($"Consume message '{result?.Name}:{result?.Message}'.");

                var eventName = result!.Name;
                var eventMessage = result!.Message;
                await ProcessEventAsync(eventName!, eventMessage!);
            }
        });
    }
    async Task ProcessEventAsync(string eventName, string eventMessage)
    {
        if (!_subscriptionsManager.HasSubscriptionsForEvent(eventName))
            return;

        foreach (var (handlerType, eventType) in _subscriptionsManager.GetEventHandlerTypes(eventName))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, handlerType);
                var handleAsync = handler.GetType().GetMethod(nameof(IEventHandler<object>.HandleAsync));
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                await (Task)handleAsync.Invoke(handler,
                [
                    JsonSerializer.Deserialize(eventMessage, eventType),
                    tokenSource?.Token,
                ])!;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                _logger.LogTrace($"{handlerType.FullName}=>{eventMessage}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message} in {handlerType.FullName}=>{eventMessage}");
            }
        }
    }
    void SubscriptionsManager_OnEventRemoved(object? sender, string eventName)
    {
        if (_subscriptionsManager.IsEmpty)
        {
            _redis.GetDatabase().ListRemove(_settings.ListKey_Keys, _settings.ListKey_Channel);
            task?.Dispose();
            task = null;
        }
    }

    class EventWrapper
    {
        public string? Name { get; set; }
        public string? Message { get; set; }
    }
}
