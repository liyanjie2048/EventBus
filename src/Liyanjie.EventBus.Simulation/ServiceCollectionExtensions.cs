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
        /// <typeparam name="TEventStore"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddSimulationEventBus<TEventStore>(this IServiceCollection services)
            where TEventStore : class, IEventStore
        {
            services.AddSingleton<IEventStore, TEventStore>();
            services.AddSingleton<ISubscriptionsManager, InMemorySubscriptionsManager>();
            services.AddSingleton<IEventBus, SimulationEventBus>();

            return services;
        }
    }
}
