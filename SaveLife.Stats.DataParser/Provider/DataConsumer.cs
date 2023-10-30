using Microsoft.Extensions.Logging;
using SaveLife.Stats.Domain.Extensions;
using SaveLife.Stats.Domain.Models;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace SaveLife.Stats.DataParser.Provider
{
    public class DataConsumer
    {
        private readonly BlockingCollection<SLTransaction> _sLTransactions;
        private readonly IList<Identity> _identities;
        private readonly HashSet<string> _othersTransactions;
        private readonly ILogger _logger;
        private long _othersTransactionsCount;

        public DataConsumer(
            BlockingCollection<SLTransaction> sLTransactions,
            ILogger logger)
        {
            _sLTransactions = sLTransactions;
            _logger = logger;
            _identities = new List<Identity>();
            _othersTransactions = new HashSet<string>();
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

                    var cardNumber = TryParseCardNumber(slTransaction);
                    var fullName = cardNumber == null ? TryParseFullName(slTransaction) : null;

                    if (cardNumber == null && fullName == null)
                    {
                        _othersTransactionsCount++;
                        _othersTransactions.Add(slTransaction.Comment);
                    }

                    _identities.Add(new Identity()
                    {
                        CardNumber = cardNumber,
                        FullName = fullName,
                        Transaction = new Transaction()
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

            var persons = _identities.Where(x => x.FullName != null);
            var filePathToPersons = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"..\..\..\identities-persons.json");
            var personsStr = persons.Select(identity => identity.Serialize());
            await File.WriteAllLinesAsync(filePathToPersons, personsStr);

            var filePathToOthers = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"..\..\..\identities-others.json");
            _othersTransactions.Add($"{Environment.NewLine}-------- The total number of unknow identities {_othersTransactionsCount}");
            await File.WriteAllLinesAsync(filePathToOthers, _othersTransactions);
        }

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
