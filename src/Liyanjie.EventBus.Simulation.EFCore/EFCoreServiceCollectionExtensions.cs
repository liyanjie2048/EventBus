using System;

using Liyanjie.EventBus;

using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 
/// </summary>
public static class EFCoreServiceCollectionExtensions
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
        services.AddDbContextFactory<EFCoreContext>(optionsAction);
        services.AddSimulationEventBus<EFCoreEventQueue>();

        return services;
    }
}
