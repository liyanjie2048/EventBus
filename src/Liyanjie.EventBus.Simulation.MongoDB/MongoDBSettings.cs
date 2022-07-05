namespace Liyanjie.EventBus;

public class MongoDBSettings
{
    public string QueueName { get; set; } = "EventBus";
    public string ChannelName { get; set; } = "Default";

    public string StoreKey_Keys => $"{QueueName}_Keys";
    public string StoreKey_Channel => $"{QueueName}_{ChannelName}";
}
