namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class MongoDBEvent : SimulationEvent
{
    /// <summary>
    /// 
    /// </summary>
    public long Id { get; set; } = DateTime.UtcNow.Ticks;
}
