using Confluent.Kafka;

namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class KafkaSettings
{
    /// <summary>
    /// 
    /// </summary>
    public int RetryCountWhenPublishEvent { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public ProducerConfig? ProducerConfig { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public ConsumerConfig? ConsumerConfig { get; set; }
}
