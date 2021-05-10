using System.Linq;
using System.Threading.Tasks;

using MongoDB.Driver;

namespace Liyanjie.EventBus.Simulation.MongoDB
{
    /// <summary>
    /// 
    /// </summary>
    public class MongoDBEventStore : IEventStore
    {
        readonly MongoDBContext context;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public MongoDBEventStore(MongoDBContext context)
        {
            this.context = context;
            if (context.Events.Indexes.List().Any() == false)
            {
                context.Events.Indexes.CreateOne(new CreateIndexModel<MongoDBEventWrapper>(Builders<MongoDBEventWrapper>.IndexKeys.Ascending(_ => _.Id)));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<EventWrapper> PopAsync()
        {
            var @event = await context.Events
                .Find(Builders<MongoDBEventWrapper>.Filter.Empty)
                .SortBy(_ => _.Id)
                .FirstOrDefaultAsync();
            if (@event == null)
                return @event;

            var result = await context.Events.DeleteOneAsync(_ => _.Id == @event.Id);
            if (result.IsAcknowledged)
                return @event;

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        public async Task<bool> PushAsync(EventWrapper @event)
        {
            try
            {
                await context.Events.InsertOneAsync(new MongoDBEventWrapper
                {
                    Name = @event.Name,
                    Message = @event.Message,
                });

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
