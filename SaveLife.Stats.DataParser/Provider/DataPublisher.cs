using Confluent.Kafka;
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
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };



        public DataPublisher(
            BlockingCollection<SLTransaction> sLTransactions,
            ILogger logger)
        {
            _sLTransactions = sLTransactions;
            _logger = logger;
        }

        public Task PublishData2(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                string bootstrapServers = "localhost:9092"; // Replace with your Kafka server
                string topic = "transactions-raw"; // Replace with your topic name
                string groupId = "test-group"; // Consumer group ID

                var config = new ConsumerConfig
                {
                    BootstrapServers = bootstrapServers,
                    GroupId = groupId,
                    AutoOffsetReset = AutoOffsetReset.Earliest // Reads messages from the beginning if no offset is found
                };

                using (var consumer = new ConsumerBuilder<Ignore, string>(config).Build())
                {
                    consumer.Subscribe(topic);

                    try
                    {
                        while (true)
                        {
                            try
                            {
                                var consumeResult = consumer.Consume(cancellationToken);
                                if (consumeResult == null)
                                {
                                    Console.WriteLine($"Unexpected null message recieved!!!");
                                    continue;
                                }

                                // handle double escaped stringify JSON
                                var transactionStr = JsonSerializer.Deserialize<string>(consumeResult!.Message.Value);
                                var transaction = JsonSerializer.Deserialize<SLTransaction>(transactionStr!, _serializerOptions);
                                _sLTransactions.Add(transaction!);
                            }
                            catch (ConsumeException e)
                            {
                                Console.WriteLine($"Error consuming message: {e.Error.Reason}");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Ensure the consumer leaves the group cleanly on shutdown
                        consumer.Close();
                    }
                }
            });
        }
    }
}
