using AutoMapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveLife.Stats.Domain.Domains;
using SaveLife.Stats.Domain.Models;
using SaveLife.Stats.Indexer.Models;
using SaveLife.Stats.Indexer.Providers;

namespace SaveLife.Stats.Indexer
{
    public class PendingTransactionConsumer : BackgroundService
    {
        private readonly TransactionsQueueProvider _transactionsQueue;
        private readonly IList<Transaction> _transactions;
        private readonly ElasticsearchProvider _elasticsearchProvider;
        private readonly DataParsingDomain _dataParsingDomain;
        private readonly IMapper _mapper;
        private readonly DataSourceConfig _sourceConfig;
        private readonly ILogger<PendingTransactionConsumer> _logger;

        public PendingTransactionConsumer(
            TransactionsQueueProvider transactionsQueue,
            ElasticsearchProvider elasticsearchProvider,
            DataParsingDomain dataParsingDomain,
            IMapper mapper,
            IOptions<DataSourceConfig> sourceConfigOptions,
            ILogger<PendingTransactionConsumer> logger)
        {
            _transactionsQueue = transactionsQueue;
            _elasticsearchProvider = elasticsearchProvider;
            _dataParsingDomain = dataParsingDomain;
            _logger = logger;
            _transactions = new List<Transaction>();
            _mapper = mapper;
            _sourceConfig = sourceConfigOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogWarning($"Starting {nameof(PendingTransactionConsumer)}");
            int batch = 1;

            while (!stoppingToken.IsCancellationRequested)
            {
                await foreach (var slTransaction in _transactionsQueue.Reader.ReadAllAsync())
                {
                    var identity = _dataParsingDomain.TryParseIdentity(slTransaction);

                    var transaction = _mapper.Map<Transaction>(slTransaction);
                    transaction.Identity = identity.Id;
                    transaction.CardNumber = identity.CardNumber;
                    transaction.FullName = identity.FullName;
                    transaction.LegalName = identity.LegalName;

                    _transactions.Add(transaction);

                    if (_transactions.Count >= _sourceConfig.BatchSize)
                    {
                        batch += 1;
                        await UpsertTransactionsBatchAsync(batch);
                    }

                }

                if(_transactions.Count > 0)
                {
                    await UpsertTransactionsBatchAsync(batch);
                }
            }
        }

        private async Task UpsertTransactionsBatchAsync(int batch)
        {
            _logger.LogInformation($"[{DateTime.Now}]: Transactions with {_transactions.Count} items reached indexing threshold. Indexing...");
            await _elasticsearchProvider.BulkUpsertAsync(_transactions);
            _transactions.Clear();
            _logger.LogInformation($"[{DateTime.Now}]: Indexing completed ({batch}/{_sourceConfig.PublisherMaxInterations})");
        }
    }
    
}
