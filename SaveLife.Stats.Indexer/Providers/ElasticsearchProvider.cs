using Elasticsearch.Net;
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
                .Index(ESConstants.TransactionsIndexAliasName)
                .UpdateMany(transactions, (bu, d) => bu.Doc(d).DocAsUpsert(true)));

            if(bulkResponse.Errors)
            {
                var failedItemsIds = bulkResponse.ItemsWithErrors.Select(x => x.Id);
                _logger.LogError($"Bulk operation completed with errors {bulkResponse.ServerError}. Failed items ids: [{string.Join(',', failedItemsIds)}]");
            }
        }

        public async Task<(IList<Donator>, CompositeKey?)> AggregateDonators(CompositeKey? afterKey)
        {
            var benefactors = new List<Donator>();
            var searchDescriptor = new SearchDescriptor<Transaction>()
                .Index(ESConstants.TransactionsIndexAliasName)
                .Size(0).TrackTotalHits(false)
                .Aggregations(a => a.Composite("identities_composite", q => q
                    .Sources(s => s
                        .Terms("identities_composite", t => t.Field(f => f.Identity.Suffix("keyword"))))
                    .After(afterKey)
                    .Size(1000)
                    .Aggregations(a => a
                        .Sum("total_amount", s => s.Field(f => f.Amount))
                        .ScriptedMetric("transactions", s => s
                            .InitScript("state.transactions = []")
                            .MapScript("state.transactions.add(doc.id)")
                            .CombineScript("def transactions = []; for (t in state.transactions) { transactions.add(t[0]) } return transactions")
                            .ReduceScript("return states")))
                ));

            var json = _client.RequestResponseSerializer.SerializeToString(searchDescriptor, SerializationFormatting.Indented);

            var response = await _client.SearchAsync<Transaction>(searchDescriptor);
            var aggregation = response.Aggregations.Composite("identities_composite");
            var buckets = aggregation?.Buckets ?? new List<CompositeBucket>();

            foreach (var item in buckets)
            {
                item.Key.TryGetValue("identities_composite", out string key);
                benefactors.Add(new Donator()
                {
                    Identity = key,
                    TotalDonation = ((ValueAggregate)item["total_amount"]).Value ?? -1,
                    TransactionIds = item.ScriptedMetric("transactions").Value<long[][]>()[0]
                });
            }

            return (benefactors, aggregation?.AfterKey);
        }
    }

    public static class DonatorAggregation
    {
        public static string CompositeName = "identities_composite";
        public static string CompositeSource = "identities_composite";
        public static string NestedTotalAmountField = "total_amount";
    }
}
