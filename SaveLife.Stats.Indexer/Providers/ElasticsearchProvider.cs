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

        public async Task<(IList<Benefactor>, CompositeKey?)> GetBenefactorsCompositeAggregation(CompositeKey? afterKey)
        {
            var benefactors = new List<Benefactor>();
            var searchDescriptor = new SearchDescriptor<Transaction>()
                .Index(ESConstants.TransactionsIndexAliasName)
                .Size(0).TrackTotalHits(false)
                .Aggregations(a => a.Composite(BenefactorsAggregation.CompositeName, q => q
                    .Sources(s => s
                        .Terms(BenefactorsAggregation.CompositeSource, t => t.Field(f => f.Identity.Suffix("keyword"))))
                    .After(afterKey)
                    .Size(1000)
                    .Aggregations(a => a.Sum(BenefactorsAggregation.NestedTotalAmountField, s => s.Field(f => f.Amount)))
                ));

            var json = _client.RequestResponseSerializer.SerializeToString(searchDescriptor, SerializationFormatting.Indented);

            var response = await _client.SearchAsync<Transaction>(searchDescriptor);
            var aggregation = response.Aggregations.Composite(BenefactorsAggregation.CompositeName);
            var buckets = aggregation?.Buckets ?? new List<CompositeBucket>();

            foreach (var item in buckets)
            {
                item.Key.TryGetValue(BenefactorsAggregation.CompositeSource, out string key);
                benefactors.Add(new Benefactor()
                {
                    Identity = key,
                    DonationCount = item.DocCount ?? -1,
                    TotalDonation = item.Sum(BenefactorsAggregation.NestedTotalAmountField).Value ?? -1
                });
            }

            return (benefactors, aggregation?.AfterKey);
        }
    }

    public static class BenefactorsAggregation
    {
        public static string CompositeName = "identities_composite";
        public static string CompositeSource = "identities_composite";
        public static string NestedTotalAmountField = "total_amount";
    }
}
