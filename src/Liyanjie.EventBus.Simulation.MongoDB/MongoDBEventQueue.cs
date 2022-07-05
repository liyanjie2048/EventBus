using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class MongoDBEventQueue : ISimulationEventQueue
{
    readonly MongoDBSettings _settings;
    readonly MongoDBContext _context;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <param name="context"></param>
    public MongoDBEventQueue(
        IOptions<MongoDBSettings> options,
        MongoDBContext context)
    {
        _settings = options.Value;
        _context = context;
    }

    public void AddChannel()
    {
        _context.Database.GetCollection<string>(_settings.StoreKey_Keys)
            .ReplaceOne(_ => _ == _settings.StoreKey_Channel,
                _settings.StoreKey_Channel,
                new ReplaceOptions() { IsUpsert = true });
    }

    public void RemoveChannel()
    {
        _context.Database.GetCollection<string>(_settings.StoreKey_Keys)
            .DeleteOne(_ => _ == _settings.StoreKey_Channel);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public async Task<bool> PushAsync(SimulationEvent @event)
    {
        var channels = await _context.Database.GetCollection<string>(_settings.StoreKey_Keys).AsQueryable()
            .ToListAsync();
        var collectionNames = await _context.Database.ListCollectionNames().ToListAsync();
        foreach (var item in channels)
        {
            var isNewCollection = !collectionNames.Contains(item);
            await _context.Database.GetCollection<MongoDBEvent>(item)
                .InsertOneAsync(new()
                {
                    Name = @event.Name,
                    EventData = @event.EventData,
                });
            if (isNewCollection)
            {
                _context.Database.GetCollection<MongoDBEvent>(item).Indexes
                    .CreateOne(new CreateIndexModel<MongoDBEvent>(Builders<MongoDBEvent>.IndexKeys.Ascending(_ => _.Id)));
            }
        }

        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<SimulationEvent?> PopAsync()
    {
        return await _context.Database.GetCollection<MongoDBEvent>(_settings.StoreKey_Channel)
            .FindOneAndDeleteAsync<MongoDBEvent>(Builders<MongoDBEvent>.Filter.Empty,
                new FindOneAndDeleteOptions<MongoDBEvent, MongoDBEvent>
                {
                    Sort = Builders<MongoDBEvent>.Sort.Ascending(_ => _.Id),
                });
    }
}
