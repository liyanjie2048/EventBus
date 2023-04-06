namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public interface ISimulationEventQueue
{
    void AddChannel();
    void RemoveChannel();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    Task<bool> PushAsync(SimulationEvent @event);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task<SimulationEvent?> PopAsync();
}
