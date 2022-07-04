
using Liyanjie.EventBus;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 
/// </summary>
public static class MongoDBServiceCollectionExtensions
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
        services.AddSimulationEventBus<MongoDBEventQueue>();

        return services;
    }
}
