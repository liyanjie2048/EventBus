# EventBus

事件总线

- #### Liyanjie.EventBus
  - IEventBus
  - IEventHandler&lt;TEvent&gt;
  - ISubscriptionsManager
  - Extension Methods
    ```csharp
    //services is IServiceCollection
    services.AddInMemoryEventBus();
    ```
- #### Liyanjie.EventBus.Kafka
  - Extension Methods
    ```csharp
    //services is IServiceCollection
    services.AddKafkaEventBus(Action<KafkaSettings> configureOptions);
    ```
- #### Liyanjie.EventBus.RabbitMQ
  - Extension Methods
    ```csharp
    //services is IServiceCollection
    services.AddRabbitMQEventBus(Action<RabbitMQSettings> configureOptions);
    ```
- #### Liyanjie.EventBus.Redis
  - Extension Methods
    ```csharp
    //services is IServiceCollection
    services.AddRedisEventBus(Action<RedisSettings> configureOptions);
    ```
- #### Liyanjie.EventBus.Simulation
  - Extension Methods
    ```csharp
    //services is IServiceCollection
    services.AddSimulationEventBus<TEventQueue>()
        where TEventQueue : class, ISimulationEventQueue;
    ```
- #### Liyanjie.EventBus.Simulation.EFCore
  - Extension Methods
    ```csharp
    //services is IServiceCollection
    services.AddEFCoreSimulationEventBus<TEventQueue>(Action<DbContextOptionsBuilder> optionsAction);
    ```
- #### Liyanjie.EventBus.Simulation.MongoDB
  - Extension Methods
    ```csharp
    //services is IServiceCollection
    services.AddMongoDBSimulationEventBus<TEventQueue>(string mongoDBConnectionString);
    ```