namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class DefaultRabbitMQPersistentConnection(
    ILogger<DefaultRabbitMQPersistentConnection> logger,
    IOptions<RabbitMQSettings> options)
    : IRabbitMQPersistentConnection
{
    readonly RabbitMQSettings _settings = options.Value;
    readonly object sync_root = new();

    IConnection? connection;
    IModel? model;
    bool disposed;

    /// <summary>
    /// 
    /// </summary>
    public bool IsConnected => connection is not null && connection.IsOpen && !disposed;

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IModel CreateModel()
    {
        return IsConnected
            ? model ??= connection!.CreateModel()
            : throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
    }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;

        try
        {
            connection?.Dispose();
        }
        catch (IOException ex)
        {
            logger.LogCritical(ex.ToString());
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool TryConnect()
    {
        logger.LogInformation("RabbitMQ Client is trying to connect");

        lock (sync_root)
        {
            Policy
                .Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(_settings.RetryCountWehnConnecting, retryAttempt => TimeSpan.FromSeconds(1), (exception, time) =>
                {
                    logger.LogWarning(exception.ToString());
                })
                .Execute(() =>
                {
                    connection = (_settings.ConnectionFactory ?? throw new ArgumentNullException(nameof(_settings.ConnectionFactory))).CreateConnection();
                });

            if (IsConnected)
            {
                connection!.ConnectionShutdown += OnConnectionShutdown;
                connection!.CallbackException += OnCallbackException;
                connection!.ConnectionBlocked += OnConnectionBlocked;

                logger.LogInformation($"RabbitMQ persistent connection acquired a connection {connection.Endpoint.HostName} and is subscribed to failure events");

                return true;
            }
            else
            {
                logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");

                return false;
            }
        }
    }

    void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        if (disposed) return;

        logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

        TryConnect();
    }
    void OnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        if (disposed) return;

        logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

        TryConnect();
    }
    void OnConnectionShutdown(object? sender, ShutdownEventArgs reason)
    {
        if (disposed) return;

        logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

        TryConnect();
    }
}
