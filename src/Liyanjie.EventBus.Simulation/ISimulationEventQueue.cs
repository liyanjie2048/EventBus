using System.Threading.Tasks;

namespace Liyanjie.EventBus
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISimulationEventQueue
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="event"></param>
        Task<bool> PushAsync(SimulationEvent @event);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<SimulationEvent> PopAsync();
    }
}
