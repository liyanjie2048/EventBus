using System.Linq;
using System.Threading.Tasks;

using MongoDB.Driver;

namespace Liyanjie.EventBus.Simulation.MongoDB
{
    /// <summary>
    /// 
    /// </summary>
    public class MongoDBEventQueue : IEventQueue
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
        public async Task<EventWrapper> PopAsync()
        {
            var @event = await context.Events
                .Find(Builders<MongoDBEventWrapper>.Filter.Where(_ => _.IsHandled == false))
                .SortBy(_ => _.Id)
                .FirstOrDefaultAsync();
            return @event == null
                ? @event
                : await context.Events.FindOneAndUpdateAsync(_ => _.Id == @event.Id, Builders<MongoDBEventWrapper>.Update
                    .Set(_ => _.IsHandled, true));
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
