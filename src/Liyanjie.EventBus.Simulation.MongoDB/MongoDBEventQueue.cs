using System.Threading.Tasks;

using MongoDB.Driver;

namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class MongoDBEventQueue : ISimulationEventQueue
{
    readonly MongoDBContext context;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    public MongoDBEventQueue(MongoDBContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<SimulationEvent> PopAsync()
    {
        var @event = await context.Events
            .Find(Builders<MongoDBEvent>.Filter.Where(_ => _.IsHandled == false))
            .SortBy(_ => _.Id)
            .FirstOrDefaultAsync();
        return @event == null
            ? @event
            : await context.Events.FindOneAndUpdateAsync(_ => _.Id == @event.Id, Builders<MongoDBEvent>.Update
                .Set(_ => _.IsHandled, true));
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
            await context.Events.InsertOneAsync(new MongoDBEvent
            {
                Name = @event.Name,
                EventData = @event.EventData,
            });

            return true;
        }
        catch
        {
            return false;
        }
    }
}
