using AutoMapper;
using Microsoft.Extensions.Logging;
using SaveLife.Stats.Domain.Models;
using SaveLife.Stats.Indexer.Constants;
using SaveLife.Stats.Indexer.Providers;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace SaveLife.Stats.Indexer.Provider
{
    public class TransactionsHandler
    {
        private readonly BlockingCollection<SLTransaction> _sLTransactions;
        private readonly IList<Transaction> _transactions;
        private readonly ElasticsearchProvider _elasticsearchProvider;
        private readonly IMapper _mapper;
        private readonly ILogger<TransactionsHandler> _logger;

        public TransactionsHandler(
            BlockingCollection<SLTransaction> sLTransactions,
            ElasticsearchProvider elasticsearchProvider,
            IMapper mapper,
            ILogger<TransactionsHandler> logger)
        {
            _sLTransactions = sLTransactions;
            _elasticsearchProvider = elasticsearchProvider;
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

                    var cardNumber = TryParseCardNumber(slTransaction);
                    var fullName = cardNumber == null ? TryParseFullName(slTransaction) : null;

                    var transaction = _mapper.Map<Transaction>(slTransaction);
                    transaction.CardNumber = cardNumber;
                    transaction.FullName = fullName;
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

        // use TryParseCardNumber & TryParseFullName from DataParser

        public string? TryParseCardNumber(SLTransaction slTransaction)
        {
            Regex rx = new Regex(@"\*\*\*\d{4}");
            MatchCollection matches = rx.Matches(slTransaction.Comment);
            return matches.FirstOrDefault()?.Value;
        }

        public string? TryParseFullName(SLTransaction slTransaction)
        {
            Regex fullNamePattern = new Regex("([IІЖЄЇА-Я]['iіжєїa-я]+ [IІЖЄЇА-Я]['iіжєїa-я]+)|" +
                @"([IІЖЄЇА-Я]['iіжєїa-я]+ [IІЖЄЇА-Я]\.\s*[IІЖЄЇА-Я]\.)|" +
                "([A-Z]\\w{2,} [A-Z]\\w{2,})");
            MatchCollection matches = fullNamePattern.Matches(slTransaction.Comment);
            var fullName = matches.FirstOrDefault()?.Value;

            if (fullName == null || fullName.ToLower() == "повернись живим")
            {
                return null;
            }

            return fullName;
        }
    }
}
