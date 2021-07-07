using System;

namespace Liyanjie.EventBus
{
    /// <summary>
    /// 
    /// </summary>
    public class EFCoreEvent : SimulationEvent
    {
        /// <summary>
        /// 
        /// </summary>
        public long Id { get; set; } = DateTime.UtcNow.Ticks;

        /// <summary>
        /// 
        /// </summary>
        public bool IsHandled { get; set; }
    }
}
