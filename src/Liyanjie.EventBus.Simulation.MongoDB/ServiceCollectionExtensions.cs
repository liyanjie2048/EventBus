using System;

using Liyanjie.EventBus;
using Liyanjie.EventBus.Simulation;
using Liyanjie.EventBus.Simulation.MongoDB;

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
        /// <param name="mongoDBConnectionString"></param>
        /// <returns></returns>
        public static IServiceCollection AddMongoDBSimulationEventBus(this IServiceCollection services, string mongoDBConnectionString)
        {
            services.AddTransient(services => new MongoDBContext(mongoDBConnectionString));
            services.AddSimulationEventBus<MongoDBEventStore>();

            return services;
        }
    }
}
