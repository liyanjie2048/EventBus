using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Confluent.Kafka;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;
using Polly.Retry;

namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class KafkaEventBus : IEventBus, IDisposable
{
    readonly ILogger<KafkaEventBus> _logger;
    readonly KafkaSettings _settings;
    readonly ISubscriptionsManager _subscriptionsManager;
    readonly IServiceProvider _serviceProvider;
    readonly AsyncRetryPolicy _policy;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="options"></param>
    /// <param name="subscriptionsManager"></param>
    /// <param name="serviceProvider"></param>
    public KafkaEventBus(
        ILogger<KafkaEventBus> logger,
        IOptions<KafkaSettings> options,
        ISubscriptionsManager subscriptionsManager,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _settings = options.Value;
        _subscriptionsManager = subscriptionsManager ?? new InMemorySubscriptionsManager();
        _serviceProvider = serviceProvider;
        _policy = Policy
            .Handle<ProduceException<Guid, object>>()
            .WaitAndRetryAsync(_settings.RetryCountWhenPublishEvent, retryAttempt => TimeSpan.FromSeconds(2), (exception, time) =>
            {
                logger.LogWarning(exception.ToString());
            });
        _subscriptionsManager.OnEventRemoved += SubscriptionsManager_OnEventRemoved;

        DoConsume();
    }

    string GetTopic<TEvent>() => typeof(TEvent).Name;

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
        using var producer = new ProducerBuilder<Guid, string>(_settings.ProducerConfig ?? throw new ArgumentNullException(nameof(_settings.ProducerConfig))).Build();
        var result = await _policy.ExecuteAsync(async () => await producer.ProduceAsync(GetTopic<TEvent>(), new Message<Guid, string>
        {
            Key = Guid.NewGuid(),
            Value = JsonSerializer.Serialize(eventData),
        }, cancellationToken));
        if (result.Status == PersistenceStatus.Persisted)
        {
            _logger.LogInformation($"Publish event success,status:{result.Status},offset:{result.Offset}");

            if (task == null)
                DoConsume();

            return true;
        }
        else
        {
            _logger.LogError($"Publish event failed,status:{result.Status},message:{result.Message}");
            return false;
        }
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

            using var consumer = new ConsumerBuilder<Guid, string>(_settings.ConsumerConfig ?? throw new ArgumentNullException(nameof(_settings.ConsumerConfig))).Build();
            consumer.Subscribe(_subscriptionsManager.GetEventNames());

            while (!cancellationToken.IsCancellationRequested)
            {
                ConsumeResult<Guid, string>? result = default;
                try
                {
                    result = consumer.Consume(cancellationToken);
                }
                catch (ConsumeException e)
                {
                    _logger.LogError($"Error occured: {e.Error.Reason}");
                    continue;
                }

                _logger.LogInformation($"Consumed message '{result.Message}' at '{result.TopicPartitionOffset}'.");

                var eventName = result.Topic;
                var eventMessage = result.Message.Value;
                await ProcessEventAsync(eventName, eventMessage);
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
            task?.Dispose();
            task = null;
        }
    }
}
