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

        public async Task UpsertDonatorsAsync(IEnumerable<DonatorEntity> donators)
        {
            var donatorIds = donators.Select(x => x.Id).ToList();
            var filter = Builders<DonatorEntity>.Filter.Where(x => donatorIds.Contains(x.Id));
            var existingItems = await Collection.Find(filter).ToListAsync();

            existingItems ??= new List<DonatorEntity> ();
            foreach (var existingItem in existingItems)
            {
                var donator = donators.First(x => x.Id == existingItem.Id);
                if(existingItem.LastTransactionStamp < donator.LastTransactionStamp)
                {
                    donator.TransactionsCount += existingItem.TransactionsCount;
                    donator.TotalDonation += existingItem.TotalDonation;
                }
            }

            var requests = donators.Select(replacement => new ReplaceOneModel<DonatorEntity>(
                filter: new ExpressionFilterDefinition<DonatorEntity>(projection => projection.Id == replacement.Id),
                replacement: replacement)
                { IsUpsert = true });

            await Collection.BulkWriteAsync(requests: requests);
        }
    }
}
