using Microsoft.Extensions.Logging;
using SaveLife.Stats.Domain.Models;
using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace SaveLife.Stats.DataParser.Provider
{
    public class DataPublisher
    {
        private readonly BlockingCollection<SLTransaction> _sLTransactions;
        private const string PROJECT_BASE_PATH = @"..\..\..";
        private const int BATCH_SIZE = 1000;
        private const int MAX_ITERATIONS_COUNT = 100;
        private readonly ILogger _logger;



        public DataPublisher(
            BlockingCollection<SLTransaction> sLTransactions,
            ILogger logger)
        {
            _sLTransactions = sLTransactions;
            _logger = logger;
        }

        public Task PublishData()
        {
            var iteration = 1;
            var readItemsCount = 0;
            return Task.Run(() =>
            {
                var readingCompleted = false;
                do
                {
                    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"{PROJECT_BASE_PATH}\..\SaveLife.Stats.Data\Raw\transactions_1-2023.json");
                    var lines = File.ReadLines(filePath).Skip(readItemsCount).Take(BATCH_SIZE);
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
                    readingCompleted = !transactions.Any() || iteration >= MAX_ITERATIONS_COUNT;
                    _logger.LogInformation($"The items was added Total: [{readItemsCount + BATCH_SIZE}]");

                } while (!readingCompleted);

                _logger.LogInformation($"Completed publishing");
                _sLTransactions.CompleteAdding();
            });
        }
    }
}
