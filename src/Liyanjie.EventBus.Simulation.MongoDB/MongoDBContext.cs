namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class MongoDBContext
{
    readonly IMongoClient _client;
    readonly IMongoDatabase _database;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="connectionString"></param>
    public MongoDBContext(string connectionString)
    {
        var mongoUrl = new MongoUrlBuilder(connectionString).ToMongoUrl();
        _client = new MongoClient(mongoUrl);
        _database = _client.GetDatabase(mongoUrl.DatabaseName);
    }

    internal IMongoDatabase Database => _database;
}
