using System;

namespace Liyanjie.EventBus.Simulation.EFCore
{
    /// <summary>
    /// 
    /// </summary>
    public class EFCoreEventWrapper : EventWrapper
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
