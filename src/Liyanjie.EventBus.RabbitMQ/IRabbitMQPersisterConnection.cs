using System;

using RabbitMQ.Client;

namespace Liyanjie.EventBus
{
    /// <summary>
    /// 
    /// </summary>
    public interface IRabbitMQPersistentConnection : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        bool TryConnect();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IModel CreateModel();
    }
}
