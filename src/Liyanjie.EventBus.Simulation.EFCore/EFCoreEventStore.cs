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
    public class EFCoreEventStore : IEventStore
    {
        readonly IServiceProvider serviceProvider;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        public EFCoreEventStore(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<EventWrapper> PopAsync()
        {
            try
            {
                using var context = serviceProvider.GetRequiredService<EFCoreContext>();
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
                using var context = serviceProvider.GetRequiredService<EFCoreContext>();
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
