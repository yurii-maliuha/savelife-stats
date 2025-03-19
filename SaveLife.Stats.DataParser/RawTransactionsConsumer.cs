using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaveLife.Stats.DataParser.Provider;
using SaveLife.Stats.Domain.Models;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace SaveLife.Stats.DataParser
{
    public class RawTransactionsConsumer(
        TransactionsQueueProvider transactionsQueue,
        ILogger<RawTransactionsConsumer> logger) : BackgroundService
    {
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogWarning($"Starting {nameof(RawTransactionsConsumer)}");

            string bootstrapServers = "localhost:9092"; // Replace with your Kafka server
            string topic = "transactions-raw"; // Replace with your topic name
            string groupId = "test-group"; // Consumer group ID

            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest, // Reads messages from the beginning if no offset is found
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe(topic);

            try
            {
                while (true)
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    if (consumeResult == null)
                    {
                        logger.LogWarning($"Unexpected null message recieved!!!");
                        continue;
                    }

                    try
                    {
                        // handle double escaped stringify JSON
                        var transactionStr = JsonSerializer.Deserialize<string>(consumeResult!.Message.Value);
                        if (string.IsNullOrEmpty(transactionStr))
                        {
                            logger.LogWarning($"Skipping empty message");
                            continue;
                        }
                        var transaction = JsonSerializer.Deserialize<SLTransaction>(transactionStr!, _serializerOptions);
                        await transactionsQueue.Writer.WriteAsync(transaction!, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error consuming message: {Message}, {Error}", consumeResult!.Message.Value, ex.Message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ensure the consumer leaves the group cleanly on shutdown
                consumer.Close();
            }
        }
    }
}
