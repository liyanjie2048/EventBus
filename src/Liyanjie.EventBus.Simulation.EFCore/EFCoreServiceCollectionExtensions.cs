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
    /// <param name="configureOptions"></param>
    /// <returns></returns>
    public static IServiceCollection AddEFCoreSimulationEventBus(this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction,
        Action<EFCoreSettings> configureOptions)
    {
        services.Configure(configureOptions ?? throw new ArgumentNullException(nameof(configureOptions)));
        services.AddDbContextFactory<EFCoreContext>(optionsAction);
        services.AddSimulationEventBus<EFCoreEventQueue>();

        return services;
    }
}
