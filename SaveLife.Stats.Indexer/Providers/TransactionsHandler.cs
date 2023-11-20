using AutoMapper;
using Microsoft.Extensions.Logging;
using SaveLife.Stats.Domain.Domains;
using SaveLife.Stats.Domain.Models;
using SaveLife.Stats.Indexer.Constants;
using SaveLife.Stats.Indexer.Providers;
using System.Collections.Concurrent;

namespace SaveLife.Stats.Indexer.Provider
{
    public class TransactionsHandler
    {
        private readonly BlockingCollection<SLTransaction> _sLTransactions;
        private readonly IList<Transaction> _transactions;
        private readonly ElasticsearchProvider _elasticsearchProvider;
        private readonly DataParsingDomain _dataParsingDomain;
        private readonly IMapper _mapper;
        private readonly ILogger<TransactionsHandler> _logger;

        public TransactionsHandler(
            BlockingCollection<SLTransaction> sLTransactions,
            ElasticsearchProvider elasticsearchProvider,
            DataParsingDomain dataParsingDomain,
            IMapper mapper,
            ILogger<TransactionsHandler> logger)
        {
            _sLTransactions = sLTransactions;
            _elasticsearchProvider = elasticsearchProvider;
            _dataParsingDomain = dataParsingDomain;
            _logger = logger;
            _transactions = new List<Transaction>();
            _mapper = mapper;
        }

        public Task ConsumeTransactions()
        {
            return Task.Run(() =>
            {
                while (!_sLTransactions.IsCompleted)
                {
                    _sLTransactions.TryTake(out SLTransaction? slTransaction);
                    if (slTransaction == null)
                    {
                        continue;
                    }

                    var cardNumber = _dataParsingDomain.TryParseCardNumber(slTransaction);
                    var fullName = cardNumber == null ? _dataParsingDomain.TryParseFullName(slTransaction) : null;
                    var legalName = cardNumber == null && fullName == null ? _dataParsingDomain.TryParseLegalName(slTransaction) : null;

                    var transaction = _mapper.Map<Transaction>(slTransaction);
                    transaction.CardNumber = cardNumber;
                    transaction.FullName = fullName;
                    transaction.LegalName = legalName;

                    _transactions.Add(transaction);

                    if (_transactions.Count >= IndexingSettings.PublisherBatchSize)
                    {
                        _logger.LogInformation($"[{DateTime.Now}]: Transactions with {_transactions.Count} items reached indexing threshold. Indexing...");
                        _elasticsearchProvider.IndexInParrarel(_transactions);
                        _transactions.Clear();
                        _logger.LogInformation($"[{DateTime.Now}]: Indexing completed");
                    }

                }

                _logger.LogInformation($"[{DateTime.Now}]: Completed consuming. Collection size {_sLTransactions.Count}");
            });
        }
    }
}
