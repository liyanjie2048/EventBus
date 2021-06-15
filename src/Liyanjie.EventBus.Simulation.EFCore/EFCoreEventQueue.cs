using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Liyanjie.EventBus.Simulation.EFCore
{
    /// <summary>
    /// 
    /// </summary>
    public class EFCoreEventQueue : IEventQueue
    {
        readonly IDbContextFactory<EFCoreContext> contextFactory;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contextFactory"></param>
        public EFCoreEventQueue(IDbContextFactory<EFCoreContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<EventWrapper> PopAsync()
        {
            try
            {
                using var context = contextFactory.CreateDbContext();
                var @event = await context.Events
                    .AsTracking()
                    .OrderBy(_ => _.Id)
                    .FirstOrDefaultAsync();
                if (@event == null)
                    return @event;

                context.Events.Remove(@event);

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
        public async Task<bool> PushAsync(EventWrapper @event)
        {
            try
            {
                using var context = contextFactory.CreateDbContext();
                context.Events.Add(new EFCoreEventWrapper
                {
                    Name = @event.Name,
                    Message = @event.Message,
                });

                return await context.SaveChangesAsync() > 0;
            }
            catch { }

            return false;
        }
    }
}
