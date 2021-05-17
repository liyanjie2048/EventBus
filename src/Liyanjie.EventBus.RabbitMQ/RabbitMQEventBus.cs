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

namespace Liyanjie.EventBus.RabbitMQ
{
    /// <summary>
    /// 
    /// </summary>
    public class RabbitMQEventBus : IEventBus, IDisposable
    {
        const string BROKER_NAME = "event_bus";

        readonly ILogger<RabbitMQEventBus> logger;
        readonly RabbitMQSettings settings;
        readonly ISubscriptionsManager subscriptionsManager;
        readonly IServiceProvider serviceProvider;
        readonly IRabbitMQPersistentConnection connection;
        readonly Policy policy;

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
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.subscriptionsManager = subscriptionsManager ?? new InMemorySubscriptionsManager();
            this.serviceProvider = serviceProvider;
            this.connection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
            this.policy = Policy
                .Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(settings.RetryCountWhenPublishEvent, retryAttempt => TimeSpan.FromSeconds(1), (exception, time) =>
                {
                    logger.LogWarning(exception.ToString());
                });
            this.subscriptionsManager.OnEventRemoved += SubscriptionsManager_OnEventRemoved;

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
            var eventName = subscriptionsManager.GetEventKey<TEvent>();
            if (subscriptionsManager.HasSubscriptions(eventName))
                return;

            if (!connection.IsConnected)
                connection.TryConnect();

            using var model = connection.CreateModel();
            model.QueueBind(
                queue: settings.QueueName,
                exchange: BROKER_NAME,
                routingKey: eventName);

            subscriptionsManager.AddSubscription<TEvent, TEventHandler>();

            if (consumerModel?.IsClosed != true)
                DoConsume();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <typeparam name="TEventHandler"></typeparam>
        public void RemoveEventHandler<TEvent, TEventHandler>()
            where TEventHandler : IEventHandler<TEvent>
        {
            subscriptionsManager.RemoveSubscription<TEvent, TEventHandler>();
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
            if (!connection.IsConnected)
                connection.TryConnect();

            var eventName = @event.GetType().Name;
            var eventMessage = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(eventMessage);

            using var model = connection.CreateModel();
            model.ExchangeDeclare(exchange: BROKER_NAME, type: ExchangeType.Direct);

            policy.Execute(() =>
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

            return await Task.FromResult(true);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            consumerModel?.Dispose();
            subscriptionsManager.Clear();
        }

        IModel consumerModel;
        void DoConsume()
        {
            if (!connection.IsConnected)
                connection.TryConnect();

            consumerModel = connection.CreateModel();
            consumerModel.ExchangeDeclare(exchange: BROKER_NAME, type: ExchangeType.Direct);
            consumerModel.QueueDeclare(
                queue: settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var consumer = new EventingBasicConsumer(consumerModel);
            consumer.Received += async (obj, e) =>
            {
                var eventName = e.RoutingKey;
                var eventMessage = Encoding.UTF8.GetString(e.Body.ToArray());

                await ProcessEventAsync(eventName, eventMessage);

                consumerModel.BasicAck(e.DeliveryTag, multiple: false);
            };

            consumerModel.BasicConsume(
                queue: settings.QueueName,
                autoAck: false,
                consumer: consumer);
            consumerModel.CallbackException += (sender, ea) =>
            {
                consumerModel.Dispose();
                DoConsume();
            };
        }
        async Task ProcessEventAsync(string eventName, string message)
        {
            if (!subscriptionsManager.HasSubscriptions(eventName))
                return;

            var eventType = subscriptionsManager.GetEventType(eventName);
            var handlerTypes = subscriptionsManager.GetEventHandlerTypes(eventName);

            var @event = JsonSerializer.Deserialize(message, eventType);
            var handlerMethod = typeof(IEventHandler<>).MakeGenericType(eventType).GetMethod(nameof(IEventHandler<object>.HandleAsync));

            using var scope = serviceProvider.CreateScope();
            foreach (var handlerType in handlerTypes)
            {
                var handler = scope.ServiceProvider.GetService(handlerType);
                await (Task)handlerMethod.Invoke(handler, new [] { @event });
            }
        }
        void SubscriptionsManager_OnEventRemoved(object sender, string eventName)
        {
            if (!connection.IsConnected)
                connection.TryConnect();

            using var model = connection.CreateModel();
            model.QueueUnbind(
                queue: settings.QueueName,
                exchange: BROKER_NAME,
                routingKey: eventName);

            if (subscriptionsManager.IsEmpty)
                consumerModel.Close();
        }
    }
}
