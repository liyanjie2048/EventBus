using System;

namespace Liyanjie.EventBus.Simulation.MongoDB
{
    /// <summary>
    /// 
    /// </summary>
    public class MongoDBEventWrapper : EventWrapper
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
