using System;

using Liyanjie.EventBus;
using Liyanjie.EventBus.RabbitMQ;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IServiceCollection AddRabbitMQEventBus(this IServiceCollection services,
            Action<RabbitMQSettings> configureOptions)
        {
            services.Configure(configureOptions ?? throw new ArgumentNullException(nameof(configureOptions)));
            services.AddSingleton<ISubscriptionsManager, InMemorySubscriptionsManager>();
            services.AddSingleton<IRabbitMQPersistentConnection, DefaultRabbitMQPersistentConnection>();
            services.AddSingleton<IEventBus, RabbitMQEventBus>();

            return services;
        }
    }
}
