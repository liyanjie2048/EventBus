﻿namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 
/// </summary>
public static class KafkaServiceCollectionExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configureOptions"></param>
    /// <returns></returns>
    public static IServiceCollection AddKafkaEventBus(this IServiceCollection services,
        Action<KafkaSettings> configureOptions)
    {
        services.Configure(configureOptions ?? throw new ArgumentNullException(nameof(configureOptions)));
        services.AddSingleton<ISubscriptionsManager, InMemorySubscriptionsManager>();
        services.AddSingleton<IEventBus, KafkaEventBus>();

        return services;
    }
}
