namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class EFEventQueue : ISimulationEventQueue
{
    readonly EFSettings _settings;
    readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <param name="serviceProvider"></param>
    public EFEventQueue(
        IOptions<EFSettings> options,
        IServiceProvider serviceProvider)
    {
        _settings = options.Value;
        _serviceProvider = serviceProvider.CreateScope().ServiceProvider;
    }

    public void AddChannel()
    {
        using var context = _serviceProvider.GetRequiredService<EFContext>();
        if (context.Channels.AsNoTracking().Any(_ => _.Name == _settings.ChannelName))
        {
            context.Channels.Add(new()
            {
                Name = _settings.ChannelName
            });
            context.SaveChanges();
        }
    }

    public void RemoveChannel()
    {
        using var context = _serviceProvider.GetRequiredService<EFContext>();
        if (context.Channels.AsNoTracking().Any(_ => _.Name == _settings.ChannelName))
        {
            var channel = context.Channels.Single(_ => _.Name == _settings.ChannelName);
            context.Channels.Remove(channel);
            context.SaveChanges();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public async Task<bool> PushAsync(SimulationEvent @event)
    {
        try
        {
            using var context = _serviceProvider.GetRequiredService<EFContext>();
            var channels = await context.Channels.AsNoTracking()
                .Select(_ => _.Name)
                .ToListAsync();
            foreach (var item in channels)
            {
                context.Events.Add(new EFEvent
                {
                    Name = @event.Name,
                    EventData = @event.EventData,
                    Channel = item,
                });
            }

            return await context.SaveChangesAsync() > 0;
        }
        catch (Exception) { }

        return default;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<SimulationEvent?> PopAsync()
    {
        try
        {
            using var context = _serviceProvider.GetRequiredService<EFContext>();
            var @event = await context.Events
                .Where(_ => _.Channel == _settings.ChannelName)
                .OrderBy(_ => _.Id)
                .FirstOrDefaultAsync();
            if (@event == null)
                return default;

            context.Events.Remove(@event);

            if (await context.SaveChangesAsync() > 0)
                return @event;
        }
        catch (Exception) { }

        return default;
    }
}
