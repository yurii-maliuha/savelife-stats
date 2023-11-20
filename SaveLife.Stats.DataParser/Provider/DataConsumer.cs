using Microsoft.Extensions.Logging;
using SaveLife.Stats.Domain.Domains;
using SaveLife.Stats.Domain.Extensions;
using SaveLife.Stats.Domain.Models;
using System.Collections.Concurrent;

namespace SaveLife.Stats.DataParser.Provider
{
    public class DataConsumer
    {
        private readonly BlockingCollection<SLTransaction> _sLTransactions;
        private readonly IList<Identity> _identities;
        private readonly HashSet<string> _othersTransactions;
        private readonly ILogger _logger;
        private long _othersTransactionsCount;
        private readonly DataParsingDomain _dataParsingDomain;

        public DataConsumer(
            BlockingCollection<SLTransaction> sLTransactions,
            DataParsingDomain dataParsingDomain,
            ILogger logger)
        {
            _sLTransactions = sLTransactions;
            _logger = logger;
            _identities = new List<Identity>();
            _othersTransactions = new HashSet<string>();
            _dataParsingDomain = dataParsingDomain;
        }

        public Task ConsumeData()
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

                    if (cardNumber == null && fullName == null && legalName == null)
                    {
                        _othersTransactionsCount++;
                        _othersTransactions.Add(slTransaction.Comment);
                    }

                    _identities.Add(new Identity()
                    {
                        CardNumber = cardNumber,
                        FullName = fullName,
                        LegalName = legalName,
                        Transaction = new TransactionRecord()
                        {
                            Id = slTransaction.Id,
                            Ammount = slTransaction.Amount,
                            Date = slTransaction.Date,
                            Currency = slTransaction.Currency,
                        }
                    });

                }

                _logger.LogInformation($"Completed consuming. Collection size {_sLTransactions.Count}");
            });
        }

        public async Task SaveIdenities()
        {
            var cardholders = _identities.Where(x => x.CardNumber != null);
            var filePathToCardholders = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"..\..\..\identities-cardholders.json");
            var cardholdersStr = cardholders.Select(identity => identity.Serialize());
            await File.WriteAllLinesAsync(filePathToCardholders, cardholdersStr);

            var persons = _identities.Where(x => x.FullName != null || x.LegalName != null);
            var filePathToPersons = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"..\..\..\identities-persons.json");
            var personsStr = persons.Select(identity => identity.Serialize());
            await File.WriteAllLinesAsync(filePathToPersons, personsStr);

            var filePathToOthers = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"..\..\..\identities-others.json");
            _othersTransactions.Add($"{Environment.NewLine}-------- The total number of unknow identities {_othersTransactionsCount}");
            await File.WriteAllLinesAsync(filePathToOthers, _othersTransactions);
        }
    }
}
