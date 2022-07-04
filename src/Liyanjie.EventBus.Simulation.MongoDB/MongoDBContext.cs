
using MongoDB.Driver;

namespace Liyanjie.EventBus
{
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

            Events.Indexes.CreateMany(new[]
            {
                new CreateIndexModel<MongoDBEvent>(Builders<MongoDBEvent>.IndexKeys.Ascending(_ => _.Id)),
                new CreateIndexModel<MongoDBEvent>(Builders<MongoDBEvent>.IndexKeys.Ascending(_ => _.Name)),
                new CreateIndexModel<MongoDBEvent>(Builders<MongoDBEvent>.IndexKeys.Ascending(_ => _.IsHandled)),
            });
        }

        /// <summary>
        /// 
        /// </summary>
        public IMongoCollection<MongoDBEvent> Events => _database.GetCollection<MongoDBEvent>(nameof(Events));
    }
}
