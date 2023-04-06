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
    /// <param name="implementationFactory"></param>
    /// <param name="configureOptions"></param>
    /// <returns></returns>
    public static IServiceCollection AddMongoDBSimulationEventBus(this IServiceCollection services,
        Func<IServiceProvider, MongoDBContext> implementationFactory,
        Action<MongoDBSettings> configureOptions)
    {
        services.Configure(configureOptions ?? throw new ArgumentNullException(nameof(configureOptions)));
        services.AddSingleton(implementationFactory);
        services.AddSimulationEventBus<MongoDBEventQueue>();

        return services;
    }
}
