using System;

using Liyanjie.EventBus;
using Liyanjie.EventBus.Simulation;

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
        /// <typeparam name="TEventQueue"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddSimulationEventBus<TEventQueue>(this IServiceCollection services)
            where TEventQueue : class, IEventQueue
        {
            services.AddSingleton<IEventQueue, TEventQueue>();
            services.AddSingleton<ISubscriptionsManager, InMemorySubscriptionsManager>();
            services.AddSingleton<IEventBus, SimulationEventBus>();

            return services;
        }
    }
}
