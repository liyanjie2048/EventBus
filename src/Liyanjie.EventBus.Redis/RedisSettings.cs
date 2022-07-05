namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class RedisSettings
{
    /// <summary>
    /// 
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string QueueName { get; set; } = "EventBus";

    /// <summary>
    /// 
    /// </summary>
    public string ChannelName { get; set; } = "Default";

    internal string ListKey_Keys => $"{QueueName}_Keys";
    internal string ListKey_Channel => $"{QueueName}_{ChannelName}";
}
