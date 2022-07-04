
using Liyanjie.EventBus;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 
/// </summary>
public static class SimulationServiceCollectionExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEventQueue"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddSimulationEventBus<TEventQueue>(this IServiceCollection services)
        where TEventQueue : class, ISimulationEventQueue
    {
        services.AddSingleton<ISimulationEventQueue, TEventQueue>();
        services.AddSingleton<ISubscriptionsManager, InMemorySubscriptionsManager>();
        services.AddSingleton<IEventBus, SimulationEventBus>();

        return services;
    }
}
