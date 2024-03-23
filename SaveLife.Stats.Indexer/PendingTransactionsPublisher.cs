using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveLife.Stats.Domain.Models;
using SaveLife.Stats.Indexer.Models;
using SaveLife.Stats.Indexer.Providers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace SaveLife.Stats.Indexer
{
    public class PendingTransactionsPublisher : BackgroundService
    {
        private readonly DataSourceConfig _sourceConfig;
        private readonly TransactionsQueueProvider _transactionsQueue;
        private readonly ILogger<PendingTransactionsPublisher> _logger;

        public PendingTransactionsPublisher(
            TransactionsQueueProvider transactionsQueue,
            IOptions<DataSourceConfig> sourceConfigOptions,
            ILogger<PendingTransactionsPublisher> logger)
        {
            _transactionsQueue = transactionsQueue;
            _sourceConfig = sourceConfigOptions.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var iteration = 1;
            var readItemsCount = 0;
            var readingCompleted = false;
            do
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _sourceConfig.Path);
                var lines = File.ReadLines(filePath).Skip(readItemsCount).Take(_sourceConfig.BatchSize);
                var transactions = lines.Select(t => JsonSerializer.Deserialize<SLTransaction>(t, new JsonSerializerOptions()
                {
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));

                transactions = transactions?.DistinctBy(x => x!.Id) ?? new List<SLTransaction>();
                foreach (var transaction in transactions)
                {
                    await _transactionsQueue.Writer.WriteAsync(transaction!, stoppingToken);
                }

                iteration++;
                readItemsCount += transactions.Count();
                _logger.LogInformation($"The items was added Total: [{readItemsCount + _sourceConfig.BatchSize}]");
                readingCompleted = !transactions.Any() || iteration > _sourceConfig.PublisherMaxInterations || stoppingToken.IsCancellationRequested;

            } while (!readingCompleted);

            _transactionsQueue.Writer.Complete();
        }
    }
}
