using System.Threading.Tasks;

namespace Liyanjie.EventBus.Simulation
{
    /// <summary>
    /// 
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="event"></param>
        Task<bool> PushAsync(EventWrapper @event);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<EventWrapper> PopAsync();
    }
}
