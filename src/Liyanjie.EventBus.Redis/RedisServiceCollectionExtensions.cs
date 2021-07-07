using System;

using Liyanjie.EventBus;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 
    /// </summary>
    public static class RedisServiceCollectionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IServiceCollection AddRedisEventBus(this IServiceCollection services,
            Action<RedisSettings> configureOptions)
        {
            services.Configure(configureOptions ?? throw new ArgumentNullException(nameof(configureOptions)));
            services.AddSingleton<ISubscriptionsManager, InMemorySubscriptionsManager>();
            services.AddSingleton<IEventBus, RedisEventBus>();

            return services;
        }
    }
}
