using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class EFCoreEventQueue : ISimulationEventQueue
{
    readonly IDbContextFactory<EFCoreContext> _contextFactory;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="contextFactory"></param>
    public EFCoreEventQueue(IDbContextFactory<EFCoreContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<SimulationEvent> PopAsync()
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var @event = await context.Events
                .AsTracking()
                .Where(_ => _.IsHandled == false)
                .OrderBy(_ => _.Id)
                .FirstOrDefaultAsync();
            if (@event == null)
                return default;

            @event.IsHandled = true;

            if (await context.SaveChangesAsync() > 0)
                return @event;
        }
        catch { }

        return null;
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
            context.Events.Add(new EFCoreEvent
            {
                Name = @event.Name,
                EventData = @event.EventData,
            });

            return await context.SaveChangesAsync() > 0;
        }
        catch { }

        return false;
    }
}
