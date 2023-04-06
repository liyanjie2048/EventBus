namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class RabbitMQEventBus : IEventBus, IDisposable
{
    const string BROKER_NAME = "event_bus";

    readonly ILogger<RabbitMQEventBus> _logger;
    readonly RabbitMQSettings _settings;
    readonly ISubscriptionsManager _subscriptionsManager;
    readonly IServiceProvider _serviceProvider;
    readonly IRabbitMQPersistentConnection _connection;
    readonly Policy _policy;
    readonly CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="options"></param>
    /// <param name="subscriptionsManager"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="persistentConnection"></param>
    public RabbitMQEventBus(
        ILogger<RabbitMQEventBus> logger,
        IOptions<RabbitMQSettings> options,
        ISubscriptionsManager subscriptionsManager,
        IServiceProvider serviceProvider,
        IRabbitMQPersistentConnection persistentConnection)
    {
        _logger = logger;
        _settings = options.Value;
        _subscriptionsManager = subscriptionsManager ?? new InMemorySubscriptionsManager();
        _serviceProvider = serviceProvider;
        _connection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
        _policy = Policy
            .Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(_settings.RetryCountWhenPublishEvent, retryAttempt => TimeSpan.FromSeconds(1), (exception, time) =>
            {
                logger.LogWarning(exception.ToString());
            });
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
        var eventName = _subscriptionsManager.GetEventName<TEvent>();
        if (!_subscriptionsManager.HasSubscriptionsForEvent(eventName))
        {
            if (!_connection.IsConnected)
                _connection.TryConnect();

            var model = _connection.CreateModel();
            //model.ExchangeDeclareNoWait(
            //    exchange: BROKER_NAME,
            //    type: ExchangeType.Direct);
            //model.QueueDeclareNoWait(
            //    queue: _settings.QueueName,
            //    durable: true,
            //    exclusive: false,
            //    autoDelete: false,
            //    arguments: null);
            model.QueueBindNoWait(
                queue: _settings.QueueName,
                exchange: BROKER_NAME,
                routingKey: eventName,
                arguments: null);
        }

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
        if (eventData is null)
            return false;

        if (!_connection.IsConnected)
            _connection.TryConnect();

        var eventName = _subscriptionsManager.GetEventName<TEvent>();
        var eventMessage = JsonSerializer.Serialize(eventData);
        var body = Encoding.UTF8.GetBytes(eventMessage);

        var model = _connection.CreateModel();
        model.ExchangeDeclareNoWait(
            exchange: BROKER_NAME,
            type: ExchangeType.Direct);

        _policy.Execute(() =>
        {
            var properties = model.CreateBasicProperties();
            properties.DeliveryMode = 2; // persistent

            model.BasicPublish(
                exchange: BROKER_NAME,
                routingKey: eventName,
                mandatory: true,
                basicProperties: properties,
                body: body);
        });

        if (consumerModel?.IsClosed != true)
            DoConsume();

        return await Task.FromResult(true);
    }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        consumerModel?.Dispose();
        _connection?.Dispose();
        _subscriptionsManager.Clear();
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    IModel? consumerModel;
    void DoConsume()
    {
        if (!_connection.IsConnected)
            _connection.TryConnect();

        consumerModel = _connection.CreateModel();
        consumerModel.ExchangeDeclareNoWait(exchange: BROKER_NAME, type: ExchangeType.Direct);
        consumerModel.QueueDeclareNoWait(
            queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new AsyncEventingBasicConsumer(consumerModel);
        consumer.Received += async (obj, e) =>
        {
            consumerModel.BasicAck(e.DeliveryTag, false);

            var eventName = e.RoutingKey;
            var eventMessage = Encoding.UTF8.GetString(e.Body.Span);
            await ProcessEventAsync(eventName, eventMessage);
        };

        consumerModel.BasicConsume(
            queue: _settings.QueueName,
            autoAck: false,
            consumer: consumer);
        consumerModel.CallbackException += (sender, ea) =>
        {
            consumerModel.Dispose();
            DoConsume();
        };
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
                await (Task)handleAsync.Invoke(handler, new[]
                {
                    JsonSerializer.Deserialize(eventMessage, eventType),
                    _cancellationTokenSource.Token,
                });
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
        if (!_connection.IsConnected)
            _connection.TryConnect();

        var model = _connection.CreateModel();
        model.QueueUnbind(
            queue: _settings.QueueName,
            exchange: BROKER_NAME,
            routingKey: eventName);

        if (_subscriptionsManager.IsEmpty)
            consumerModel?.Close();
    }
}
