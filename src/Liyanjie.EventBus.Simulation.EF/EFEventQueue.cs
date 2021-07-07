using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

namespace Liyanjie.EventBus
{
    /// <summary>
    /// 
    /// </summary>
    public class EFEventQueue : ISimulationEventQueue
    {
        readonly IServiceProvider serviceProvider;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        public EFEventQueue(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<SimulationEvent> PopAsync()
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<EFContext>();
                var @event = await context.Events
                    .AsNoTracking()
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
                using var scope = serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<EFContext>();
                context.Events.Add(new EFEvent
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
