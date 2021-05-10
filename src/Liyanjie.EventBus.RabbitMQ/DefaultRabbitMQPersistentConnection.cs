﻿using System;
using System.IO;
using System.Net.Sockets;

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
    public class DefaultRabbitMQPersistentConnection : IRabbitMQPersistentConnection
    {
        readonly ILogger<DefaultRabbitMQPersistentConnection> logger;
        readonly RabbitMQSettings settings;
        readonly object sync_root = new object();

        IConnection connection;
        bool disposed;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        public DefaultRabbitMQPersistentConnection(
            ILogger<DefaultRabbitMQPersistentConnection> logger,
            IOptions<RabbitMQSettings> options)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsConnected => connection != null && connection.IsOpen && !disposed;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IModel CreateModel()
        {
            return IsConnected
                ? connection.CreateModel()
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
                connection.Dispose();
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
                    .WaitAndRetry(settings.RetryCountWehnConnecting, retryAttempt => TimeSpan.FromSeconds(1), (exception, time) =>
                    {
                        logger.LogWarning(exception.ToString());
                    })
                    .Execute(() =>
                    {
                        connection = settings.Connection.CreateConnection();
                    });

                if (IsConnected)
                {
                    connection.ConnectionShutdown += OnConnectionShutdown;
                    connection.CallbackException += OnCallbackException;
                    connection.ConnectionBlocked += OnConnectionBlocked;

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

        void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (disposed) return;

            logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

            TryConnect();
        }
        void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (disposed) return;

            logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

            TryConnect();
        }
        void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (disposed) return;

            logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

            TryConnect();
        }
    }
}