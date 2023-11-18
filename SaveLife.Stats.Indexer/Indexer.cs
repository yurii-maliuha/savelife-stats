using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nest;
using SaveLife.Stats.Domain.Models;
using SaveLife.Stats.Indexer.Providers;
using System.Runtime.ExceptionServices;

namespace SaveLife.Stats.Indexer
{
    public class Indexer : BackgroundService
    {
        private readonly ElasticsearchScaffolder _elasticsearchScaffolder;
        private readonly TransactionReader _transactionReader;
        private readonly IElasticClient _searchClient;
        private readonly ILogger<Indexer> _logger;

        public Indexer(
            ElasticsearchScaffolder elasticsearchScaffolder,
            TransactionReader transactionReader,
            IElasticClient searchClient,
            ILogger<Indexer> logger)
        {
            _elasticsearchScaffolder = elasticsearchScaffolder;
            _transactionReader = transactionReader;
            _searchClient = searchClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Started loading at {DateTime.Now}");

            await _elasticsearchScaffolder.ScaffoldAsync();

            var slTransactions = _transactionReader.ReadTransactions(0, 100);

            var transactions = slTransactions.Select(x => new Transaction()
            {
                Id = x.Id,
                Amount = x.Amount,
                Comment = x.Comment,
                Currency = x.Currency,
                Date = x.Date,
                Source = x.Source
            });

            var handle = new ManualResetEvent(false);
            var seenPages = 0;
            var observableBulk = _searchClient.BulkAll(transactions, f => f
                .MaxDegreeOfParallelism(16)
                .BackOffTime(TimeSpan.FromSeconds(10))
                .BackOffRetries(2)
                .Size(25)
                .RefreshOnCompleted()
            );

            ExceptionDispatchInfo exception = null;
            var bulkObserver = new BulkAllObserver(
                onError: e =>
                {
                    exception = ExceptionDispatchInfo.Capture(e);
                    handle.Set();
                },
                onCompleted: () => handle.Set(),
                onNext: b =>
                {
                    Interlocked.Increment(ref seenPages);
                    Console.WriteLine($"indexed {seenPages} pages");
                }
            );

            observableBulk.Subscribe(bulkObserver);
            handle.WaitOne();

            if (exception != null)
                exception.Throw();


        }
    }
}
