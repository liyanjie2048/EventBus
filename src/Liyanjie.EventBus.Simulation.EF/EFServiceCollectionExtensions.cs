namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 
/// </summary>
public static class EFServiceCollectionExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="dbContextFactory"></param>
    /// <param name="configureOptions"></param>
    /// <returns></returns>
    public static IServiceCollection AddEFSimulationEventBus(this IServiceCollection services,
        Func<IServiceProvider, EFContext> dbContextFactory,
        Action<EFSettings> configureOptions)
    {
        services.Configure(configureOptions ?? throw new ArgumentNullException(nameof(configureOptions)));
        services.AddScoped<EFContext>(dbContextFactory);
        services.AddSimulationEventBus<EFEventQueue>();

        return services;
    }
}
