namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class EFCoreEvent : SimulationEvent
{
    /// <summary>
    /// 
    /// </summary>
    public long Id { get; set; } = DateTime.UtcNow.Ticks;

    public string? Channel { get; set; }
}
