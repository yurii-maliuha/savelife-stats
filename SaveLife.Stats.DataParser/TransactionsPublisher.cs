using AutoMapper;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaveLife.Stats.DataParser.Provider;
using SaveLife.Stats.Domain.Domains;
using SaveLife.Stats.Domain.Models;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace SaveLife.Stats.DataParser
{
    public class TransactionsPublisher(
        TransactionsQueueProvider transactionsQueue,
        DataParsingDomain dataParsingDomain,
        IMapper mapper,
        ILogger<TransactionsPublisher> logger) : BackgroundService
    {
        private const int BatchSize = 1;
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogWarning($"Starting {nameof(TransactionsPublisher)}");
            var transactions = new List<Transaction>();

            while (!stoppingToken.IsCancellationRequested)
            {
                await foreach (var slTransaction in transactionsQueue.Reader.ReadAllAsync())
                {
                    var identity = dataParsingDomain.TryParseIdentity(slTransaction);

                    var transaction = mapper.Map<Transaction>(slTransaction);
                    transaction.Identity = identity.Id;
                    transaction.CardNumber = identity.CardNumber;
                    transaction.FullName = identity.FullName;
                    transaction.LegalName = identity.LegalName;
                    transaction.IndexingDate = DateTime.UtcNow;

                    transactions.Add(transaction);

                    await UpsertTransactionsBatchAsync(transactions, forceUpsert: false, stoppingToken);
                }

                await UpsertTransactionsBatchAsync(transactions, forceUpsert: true, stoppingToken);
            }
        }

        private async Task UpsertTransactionsBatchAsync(List<Transaction> transactions, bool forceUpsert, CancellationToken stoppingToken)
        {
            if (transactions.Count == 0)
            {
                return;
            }

            if (transactions.Count <= BatchSize && !forceUpsert)
            {
                return;
            }

            var topic = "transactions";
            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092",
                Acks = Acks.All // Ensure messages are persisted
            };
            using var producer = new ProducerBuilder<Null, string>(config).Build();

            try
            {
                foreach (var transaction in transactions)
                {
                    var transactionJson = JsonSerializer.Serialize(transaction, _serializerOptions);
                    var message = new Message<Null, string> { Value = transactionJson };
                    await producer.ProduceAsync(topic, message, stoppingToken);
                    logger.LogWarning("Transaction processed: {ID}", transaction.Id);
                }
            }
            catch (ProduceException<Null, Transaction> e)
            {
                Console.WriteLine($"Error producing message: {e.Error.Reason}");
            }
        }
    }
}
