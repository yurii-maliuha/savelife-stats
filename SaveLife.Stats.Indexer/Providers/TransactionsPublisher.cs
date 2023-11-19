using Microsoft.Extensions.Logging;
using SaveLife.Stats.Domain.Models;
using SaveLife.Stats.Indexer.Constants;
using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace SaveLife.Stats.Indexer.Provider
{
    public class TransactionsPublisher
    {
        private readonly BlockingCollection<SLTransaction> _sLTransactions;
        private const string PROJECT_BASE_PATH = @"..\..\..";
        private readonly ILogger<TransactionsPublisher> _logger;



        public TransactionsPublisher(
            BlockingCollection<SLTransaction> sLTransactions,
            ILogger<TransactionsPublisher> logger)
        {
            _sLTransactions = sLTransactions;
            _logger = logger;
        }

        public Task PublishTransactions()
        {
            var iteration = 1;
            var readItemsCount = 0;
            return Task.Run(() =>
            {
                var readingCompleted = false;
                do
                {
                    // TODO: remove hardcoding of transactions file
                    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"{PROJECT_BASE_PATH}\..\SaveLife.Stats.Data\Raw\transactions_1-2023.json");
                    var lines = File.ReadLines(filePath).Skip(readItemsCount).Take(IndexingSettings.PublisherBatchSize);
                    var transactions = lines.Select(t => JsonSerializer.Deserialize<SLTransaction>(t, new JsonSerializerOptions()
                    {
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }));

                    foreach (var transaction in transactions)
                    {
                        _sLTransactions.Add(transaction!);
                    }

                    iteration++;
                    readItemsCount += transactions.Count();
                    readingCompleted = !transactions.Any() || iteration >= IndexingSettings.PublisherMaxInterations;
                    _logger.LogInformation($"The items was added Total: [{readItemsCount + IndexingSettings.PublisherBatchSize}]");

                } while (!readingCompleted);

                _logger.LogInformation($"Completed publishing");
                _sLTransactions.CompleteAdding();
            });
        }
    }
}
