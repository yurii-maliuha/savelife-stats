using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaveLife.Stats.Indexer.Provider;
using SaveLife.Stats.Indexer.Providers;

namespace SaveLife.Stats.Indexer
{
    public class Indexer : BackgroundService
    {
        private readonly ElasticsearchScaffolder _elasticsearchScaffolder;
        private readonly TransactionsPublisher _transactionsPublisher;
        private readonly TransactionsHandler _transactionsHandler;
        private readonly ILogger<Indexer> _logger;

        public Indexer(
            ElasticsearchScaffolder elasticsearchScaffolder,
            TransactionsPublisher transactionsPublisher,
           TransactionsHandler transactionsHandler,
            ILogger<Indexer> logger)
        {
            _elasticsearchScaffolder = elasticsearchScaffolder;
            _transactionsPublisher = transactionsPublisher;
            _transactionsHandler = transactionsHandler;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Started loading at {DateTime.Now}");

            await _elasticsearchScaffolder.ScaffoldAsync();

            await Task.WhenAll(_transactionsPublisher.PublishTransactions(), _transactionsHandler.ConsumeTransactions());

            _logger.LogInformation($"Finished loading at {DateTime.Now}");
        }
    }
}
