using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

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
        var eventName = _subscriptionsManager.GetEventKey<TEvent>();
        if (_subscriptionsManager.HasSubscriptions(eventName))
            return;

        if (!_connection.IsConnected)
            _connection.TryConnect();

        using var model = _connection.CreateModel();
        model.QueueBind(
            queue: _settings.QueueName,
            exchange: BROKER_NAME,
            routingKey: eventName);

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
        if (!_connection.IsConnected)
            _connection.TryConnect();

        var eventName = @event.GetType().Name;
        var eventMessage = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(eventMessage);

        using var model = _connection.CreateModel();
        model.ExchangeDeclare(exchange: BROKER_NAME, type: ExchangeType.Direct);

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
        _subscriptionsManager.Clear();
    }

    IModel consumerModel;
    void DoConsume()
    {
        if (!_connection.IsConnected)
            _connection.TryConnect();

        consumerModel = _connection.CreateModel();
        consumerModel.ExchangeDeclare(exchange: BROKER_NAME, type: ExchangeType.Direct);
        consumerModel.QueueDeclare(
            queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new EventingBasicConsumer(consumerModel);
        consumer.Received += async (obj, e) =>
        {
            consumerModel.BasicAck(e.DeliveryTag, false);

            var eventName = e.RoutingKey;
            var eventMessage = Encoding.UTF8.GetString(e.Body.ToArray());
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
        if (!_subscriptionsManager.HasSubscriptions(eventName))
            return;

        var eventType = _subscriptionsManager.GetEventType(eventName);
        var handlerTypes = _subscriptionsManager.GetEventHandlerTypes(eventName);

        var @event = JsonSerializer.Deserialize(eventMessage, eventType);
        var handlerMethod = typeof(IEventHandler<>).MakeGenericType(eventType).GetMethod(nameof(IEventHandler<object>.HandleAsync));

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
        if (!_connection.IsConnected)
            _connection.TryConnect();

        using var model = _connection.CreateModel();
        model.QueueUnbind(
            queue: _settings.QueueName,
            exchange: BROKER_NAME,
            routingKey: eventName);

        if (_subscriptionsManager.IsEmpty)
            consumerModel.Close();
    }
}
