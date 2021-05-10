using System;

using Liyanjie.EventBus;
using Liyanjie.EventBus.Simulation;
using Liyanjie.EventBus.Simulation.EFCore;

using Microsoft.EntityFrameworkCore;

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
        /// <param name="optionsAction"></param>
        /// <returns></returns>
        public static IServiceCollection AddEFCoreSimulationEventBus(this IServiceCollection services,
            Action<DbContextOptionsBuilder> optionsAction)
        {
            services.AddDbContext<EFCoreContext>(optionsAction, ServiceLifetime.Transient, ServiceLifetime.Singleton);
            services.AddSimulationEventBus<EFCoreEventStore>();

            return services;
        }
    }
}
