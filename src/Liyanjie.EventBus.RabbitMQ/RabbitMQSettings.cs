namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class RabbitMQSettings
{
    /// <summary>
    /// 
    /// </summary>
    public int RetryCountWhenPublishEvent { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public ConnectionFactory? ConnectionFactory { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public int RetryCountWehnConnecting { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string QueueName { get; set; } = "Default";
}
