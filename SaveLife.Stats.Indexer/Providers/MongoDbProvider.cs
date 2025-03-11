using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using SaveLife.Stats.Domain.Models;

namespace SaveLife.Stats.Indexer.Providers
{
    public class MongoDbProvider
    {
        private const string DONATORS_COLLECTION = "Donators";
        private readonly IMongoDatabase _mongoDatabase;
        protected IMongoCollection<DonatorEntity> Collection => _mongoDatabase.GetCollection<DonatorEntity>(DONATORS_COLLECTION);

        public MongoDbProvider(
            IMongoDatabase mongoDatabase)
        {
            _mongoDatabase = mongoDatabase;
        }

        public async Task UpsertDonatorsAsync(IEnumerable<Domain.Models.DonatorEntity> donators)
        {
            var donatorIds = donators.Select(x => x.Id).ToList();
            var filter = Builders<Domain.Models.DonatorEntity>.Filter.Where(x => donatorIds.Contains(x.Id));
            var existingItems = await Collection.Find(filter).ToListAsync();

            existingItems ??= new List<Domain.Models.DonatorEntity>();
            foreach (var existingItem in existingItems)
            {
                var donator = donators.First(x => x.Id == existingItem.Id);
                if(existingItem.LastTransactionStamp < donator.LastTransactionStamp)
                {
                    donator.TransactionsCount += existingItem.TransactionsCount;
                    donator.TotalDonation += existingItem.TotalDonation;
                }
            }

            var requests = donators.Select(replacement => new ReplaceOneModel<Domain.Models.DonatorEntity>(
                filter: new ExpressionFilterDefinition<Domain.Models.DonatorEntity>(projection => projection.Id == replacement.Id),
                replacement: replacement)
                { IsUpsert = true });

            await Collection.BulkWriteAsync(requests: requests);
        }

        public async Task<IEnumerable<DonatorEntity>> GetTopDonaterAsync(int size)
        {
            var query = Collection
                .Find(x => x.Identity != "Unidentified")
                .SortByDescending(x => x.TotalDonation)
                .Project(Builders<DonatorEntity>.Projection.Exclude(x => x.Id).Exclude(x => x.LastTransactionStamp))
                .Limit(size);

            var results = await query.ToListAsync();
            return Enumerable.Select<MongoDB.Bson.BsonDocument, Domain.Models.DonatorEntity>(results, (Func<MongoDB.Bson.BsonDocument, Domain.Models.DonatorEntity>)(x => BsonSerializer.Deserialize<Domain.Models.DonatorEntity>(x)));
        }
    }
}
