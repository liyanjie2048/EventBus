using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using StackExchange.Redis;

namespace Liyanjie.EventBus
{
    /// <summary>
    /// 
    /// </summary>
    public class RedisEventBus : IEventBus, IDisposable
    {
        readonly ILogger<RedisEventBus> logger;
        readonly RedisSettings settings;
        readonly ISubscriptionsManager subscriptionsManager;
        readonly IServiceProvider serviceProvider;
        readonly ConnectionMultiplexer redis;

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
            this.logger = logger;
            this.settings = options.Value;
            this.subscriptionsManager = subscriptionsManager ?? new InMemorySubscriptionsManager();
            this.serviceProvider = serviceProvider;
            this.redis = ConnectionMultiplexer.Connect(settings.ConnectionString);
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
            subscriptionsManager.AddSubscription<TEvent, TEventHandler>();
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
            var length = await redis.GetDatabase().ListRightPushAsync(settings.ListKey,
                JsonSerializer.Serialize(new EventWrapper
                {
                    Name = subscriptionsManager.GetEventKey<TEvent>(),
                    Message = JsonSerializer.Serialize(@event),
                }));

            logger.LogInformation($"Publish event success,list length:{length}");

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
            subscriptionsManager.Clear();
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
                    EventWrapper result = default;
                    try
                    {
                        var value = (string)await redis.GetDatabase().ListLeftPopAsync(settings.ListKey);
                        if (value == null)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1));
                            continue;
                        }

                        result = JsonSerializer.Deserialize<EventWrapper>(value);
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"Error occured: {e.Message}");
                        continue;
                    }

                    logger.LogInformation($"Consume message '{result.Name}:{result.Message}'.");

                    var eventName = result.Name;
                    var eventMessage = result.Message;
                    await ProcessEventAsync(eventName, eventMessage);
                }
            }, tokenSource.Token);
        }
        async Task ProcessEventAsync(string eventName, string eventMessage)
        {
            if (!subscriptionsManager.HasSubscriptions(eventName))
                return;

            var eventType = subscriptionsManager.GetEventType(eventName);
            var @event = JsonSerializer.Deserialize(eventMessage, eventType);
            var handlerMethod = typeof(IEventHandler<>).MakeGenericType(eventType).GetMethod(nameof(IEventHandler<object>.HandleAsync));

            var handlerTypes = subscriptionsManager.GetEventHandlerTypes(eventName);
            using var scope = serviceProvider.CreateScope();
            foreach (var handlerType in handlerTypes)
            {
                try
                {
                    var handler = ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, handlerType);
                    await (Task)handlerMethod.Invoke(handler, new[] { @event });
                    logger.LogDebug($"{handlerType.FullName}=>{eventMessage}");
                }
                catch (Exception ex)
                {
                    logger.LogError($"{ex.Message} in {handlerType.FullName}=>{eventMessage}");
                }
            }
        }
        void SubscriptionsManager_OnEventRemoved(object sender, string eventName)
        {
            if (subscriptionsManager.IsEmpty)
            {
                task?.Dispose();
                task = null;
            }
        }

        class EventWrapper
        {
            public string Name { get; set; }
            public string Message { get; set; }
        }
    }
}
