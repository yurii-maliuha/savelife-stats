using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nest;
using SaveLife.Stats.Domain.Extensions;
using SaveLife.Stats.Indexer.Providers;

namespace SaveLife.Stats.Indexer
{
    public class TransactionsDataAggregator : BackgroundService
    {
        private readonly ILogger<TransactionsDataAggregator> _logger;
        private readonly ElasticsearchProvider _searchProvider;

        public TransactionsDataAggregator(
            ElasticsearchProvider elasticsearchProvider,
            ILogger<TransactionsDataAggregator> logger)
        {
            _searchProvider = elasticsearchProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogWarning($"Starting {nameof(TransactionsDataAggregator)}");
            CompositeKey? afterKey = null;
            do
            {
                var (benefactors, key) = await _searchProvider.GetBenefactorsCompositeAggregation(afterKey);
                var filePathToCardholders = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"..\..\..\Data\benefactors.json");
                var benefactorsStr = benefactors.Select(identity => identity.Serialize());
                await File.AppendAllLinesAsync(filePathToCardholders, benefactorsStr);
                afterKey = key;
            } while (afterKey != null);


            _logger.LogWarning($"Finishing {nameof(TransactionsDataAggregator)}");
        }
    }
}
