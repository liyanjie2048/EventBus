using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Liyanjie.EventBus.Simulation
{
    /// <summary>
    /// 
    /// </summary>
    public class SimulationEventBus : IEventBus, IDisposable
    {
        readonly ILogger<SimulationEventBus> logger;
        readonly ISubscriptionsManager subscriptionsManager;
        readonly IEventStore eventStore;
        readonly IServiceProvider serviceProvider;

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
            IEventStore eventStore,
            IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.subscriptionsManager = subscriptionsManager ?? new InMemorySubscriptionsManager();
            this.eventStore = eventStore;
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

            if (task == null)
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
            var result = await eventStore.PushAsync(new EventWrapper
            {
                Name = subscriptionsManager.GetEventKey<TEvent>(),
                Message = JsonSerializer.Serialize(@event),
            });

            logger.LogInformation($"Publish event {(result ? "success" : "failed")}");
            return result;
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
                    try
                    {
                        if (!(await eventStore.PopAsync() is EventWrapper @event))
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1));
                            continue;
                        }

                        logger.LogInformation($"Consume message '{@event.Name}:{@event.Message}'.");

                        var eventName = @event.Name;
                        var eventMessage = @event.Message;

                        await ProcessEventAsync(eventName, eventMessage);
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"Error occured: {e.Message}");
                    }
                }
            }, tokenSource.Token);
        }
        async Task ProcessEventAsync(string eventName, string eventMessage)
        {
            var eventType = subscriptionsManager.GetEventType(eventName);
            var handlerTypes = subscriptionsManager.GetEventHandlerTypes(eventName);

            var @event = JsonSerializer.Deserialize(eventMessage, eventType);
            var handlerMethod = typeof(IEventHandler<>).MakeGenericType(eventType).GetMethod(nameof(IEventHandler<object>.HandleAsync));

            using var scope = serviceProvider.CreateScope();
            foreach (var handlerType in handlerTypes)
            {
                var handler = scope.ServiceProvider.GetService(handlerType);
                await (Task)handlerMethod.Invoke(handler, new[] { @event });
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
    }
}
