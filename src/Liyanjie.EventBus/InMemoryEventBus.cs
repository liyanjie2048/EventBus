using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Liyanjie.EventBus
{
    /// <summary>
    /// 
    /// </summary>
    public class InMemoryEventBus : IEventBus, IDisposable
    {
        readonly ILogger<InMemoryEventBus> logger;
        readonly ISubscriptionsManager subscriptionsManager;
        readonly IServiceProvider serviceProvider;
        readonly ConcurrentQueue<EventWrapper> queue = new();


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
            this.logger = logger;
            this.subscriptionsManager = subscriptionsManager ?? new InMemorySubscriptionsManager();
            this.serviceProvider = serviceProvider;
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
            await Task.CompletedTask;

            queue.Enqueue(new EventWrapper
            {
                Name = subscriptionsManager.GetEventKey<TEvent>(),
                Message = JsonSerializer.Serialize(@event),
            });

            logger.LogInformation($"Publish event success,list length:{queue.Count}");

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
            subscriptionsManager.Clear();
#if NETSTANDARD
            queue.Clear();
#endif
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
                    var result = queue.TryDequeue(out var @event) ? @event : null;
                    if (result == null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
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
