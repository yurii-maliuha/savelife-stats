using Microsoft.Extensions.Logging;
using Nest;
using SaveLife.Stats.Domain.Models;
using SaveLife.Stats.Indexer.Constants;

namespace SaveLife.Stats.Indexer.Providers
{
    public class ElasticsearchProvider
    {
        private readonly IElasticClient _client;
        private readonly ILogger<ElasticsearchProvider> _logger;

        public ElasticsearchProvider(
            IElasticClient client,
            ILogger<ElasticsearchProvider> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task BulkUpsertAsync(IEnumerable<Transaction> transactions)
        {
            var cc = transactions.DistinctBy(x => x.Id).Count();

            var bulkResponse = await _client.BulkAsync(b => b
                .Index(ElasticsearchIndexes.TransactionsIndexAliasName)
                .UpdateMany(transactions, (bu, d) => bu.Doc(d).DocAsUpsert(true)));

            if(bulkResponse.Errors)
            {
                var failedItemsIds = bulkResponse.ItemsWithErrors.Select(x => x.Id);
                _logger.LogError($"Bulk operation completed with errors {bulkResponse.ServerError}. Failed items ids: [{string.Join(',', failedItemsIds)}]");
            }
        }
    }
}
