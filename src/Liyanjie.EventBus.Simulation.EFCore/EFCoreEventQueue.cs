using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class EFCoreEventQueue : ISimulationEventQueue
{
    readonly EFCoreSettings _settings;
    readonly IDbContextFactory<EFCoreContext> _contextFactory;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <param name="contextFactory"></param>
    public EFCoreEventQueue(
        IOptions<EFCoreSettings> options,
        IDbContextFactory<EFCoreContext> contextFactory)
    {
        _settings = options.Value;
        _contextFactory = contextFactory;
    }

    public void AddChannel()
    {
        using var context = _contextFactory.CreateDbContext();
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
        using var context = _contextFactory.CreateDbContext();
        if (context.Channels.AsNoTracking().Any(_ => _.Name == _settings.ChannelName))
        {
            var channel = context.Channels.AsTracking().Single(_ => _.Name == _settings.ChannelName);
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
            using var context = _contextFactory.CreateDbContext();
            var channels = await context.Channels.AsNoTracking()
                .Select(_ => _.Name)
                .ToListAsync();
            context.Events.AddRange(channels.Select(_ => new EFCoreEvent
            {
                Name = @event.Name,
                EventData = @event.EventData,
                Channel = _,
            }));
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
            using var context = _contextFactory.CreateDbContext();
            var @event = await context.Events
                .AsTracking()
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
