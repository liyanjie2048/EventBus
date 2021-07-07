# EventBus

- #### Liyanjie.EventBus
  - IEventBus
  - IEventHandler&lt;TEvent&gt;
  - ISubscriptionsManager
  - ExtendMethods
    ```csharp
    //services is IServiceCollection
    services.AddInMemoryEventBus();
    ```
- #### Liyanjie.EventBus.Kafka
  - ExtendMethods
    ```csharp
    //services is IServiceCollection
    services.AddKafkaEventBus(Action<KafkaSettings> configureOptions);
    ```
- #### Liyanjie.EventBus.RabbitMQ
  - ExtendMethods
    ```csharp
    //services is IServiceCollection
    services.AddRabbitMQEventBus(Action<RabbitMQSettings> configureOptions);
    ```
- #### Liyanjie.EventBus.Redis
  - ExtendMethods
    ```csharp
    //services is IServiceCollection
    services.AddRedisEventBus(Action<RedisSettings> configureOptions);
    ```
- #### Liyanjie.EventBus.Simulation
  - ExtendMethods
    ```csharp
    //services is IServiceCollection
    services.AddSimulationEventBus<TEventQueue>()
        where TEventQueue : class, ISimulationEventQueue;
    ```
- #### Liyanjie.EventBus.Simulation.EF
  - ExtendMethods
    ```csharp
    //services is IServiceCollection
    services.AddEFSimulationEventBus(this IServiceCollection services,
        Func<IServiceProvider, EFContext> dbContextFactory);
    ```
- #### Liyanjie.EventBus.Simulation.EFCore
  - ExtendMethods
    ```csharp
    //services is IServiceCollection
    services.AddEFCoreSimulationEventBus<TEventQueue>(Action<DbContextOptionsBuilder> optionsAction);
    ```
- #### Liyanjie.EventBus.Simulation.MongoDB
  - ExtendMethods
    ```csharp
    //services is IServiceCollection
    services.AddMongoDBSimulationEventBus<TEventQueue>(string mongoDBConnectionString);
    ```