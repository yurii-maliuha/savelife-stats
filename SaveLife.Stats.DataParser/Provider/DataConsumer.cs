using Microsoft.Extensions.Logging;
using SaveLife.Stats.Domain.Extensions;
using SaveLife.Stats.Domain.Models;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SaveLife.Stats.DataParser.Provider
{
    public class DataConsumer
    {
        private readonly BlockingCollection<SLTransaction> _sLTransactions;
        private readonly IList<Identity> _identities;
        private readonly HashSet<string> _othersTransactions;
        private readonly HashSet<string> _knownNames;
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
            _knownNames = LoadKnownNames();
        }

        private HashSet<string> LoadKnownNames()
        {
            var namesSet = new HashSet<string>();
            string filePath = Path.Combine("Data", $"known-names.json");
            if (!File.Exists(filePath))
            {
                return new HashSet<string>();
            }

            var lines = File.ReadAllLines(filePath);
            var names = lines[0].Deserialize<IEnumerable<string>>();
            foreach (var name in names)
            {
                namesSet.Add(name.ToLowerInvariant());
                if (name.ToLowerInvariant().Contains('і'))
                {
                    namesSet.Add(name.ToLowerInvariant().Replace('і', 'i'));
                }
                if (name.ToLowerInvariant().Contains("'"))
                {
                    namesSet.Add(name.ToLowerInvariant().Replace("'", ""));
                }
            }

            return namesSet;
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

            var persons = _identities.Where(x => x.FullName != null);
            var filePathToPersons = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"..\..\..\identities-persons.json");
            var personsStr = persons.Select(identity => identity.Serialize());
            await File.WriteAllLinesAsync(filePathToPersons, personsStr);

            var filePathToOthers = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"..\..\..\identities-others.json");
            _othersTransactions.Add($"{Environment.NewLine}-------- The total number of unknow identities {_othersTransactionsCount}");
            await File.WriteAllLinesAsync(filePathToOthers, _othersTransactions);
        }

        public static string? TryParseCardNumber(SLTransaction slTransaction)
        {
            Regex rx = new Regex(@"\*\*\*\d{4}");
            MatchCollection matches = rx.Matches(slTransaction.Comment);
            return matches.FirstOrDefault()?.Value;
        }

        public string? TryParseFullName(SLTransaction slTransaction)
        {
            string? fullName = null;
            Regex legalEntityPatern = new Regex(@"(ПрАТ|ТОВ|ОСББ|ТзОВ|ФГ|ПП)\s+""\s*[IІЖЄЇА-Я\-'iіжєїa-я0-9 ]+\s*""");
            fullName = legalEntityPatern.Matches(slTransaction.Comment).LastOrDefault()?.Value.Replace(@"""", "'");

            // Прізвище Ім'я
            Regex generalFullNamePattern = new Regex(@"([IІЖЄЇА-Я][\-'iіжєїa-я]+ [IІЖЄЇА-Я]['iіжєїa-я]+)");
            var fullNameMatches = generalFullNamePattern.Matches(slTransaction.Comment).Select(x => x.Groups[0].Value);
            foreach (var possiblyFullName in fullNameMatches)
            {
                fullName ??= possiblyFullName.Split(' ').Any(x => _knownNames.Contains(x.ToLowerInvariant())) == true ? possiblyFullName : null;
            }

            // there is higher possibility that full name is defined in ukrainian 
            fullName ??= fullNameMatches.LastOrDefault();

            // Прізвище І. C. || Mr Lastname || FirstName LastName
            Regex initialsOrForeignNames = new Regex(@"([IІЖЄЇА-Я][\-'iіжєїa-я]+\s+[IІЖЄЇА-Я]\.\s*[IІЖЄЇА-Я]\.)|([mrsMRS]{2}\s+[A-Z]\w{2,})|([A-Z]\w{2,}\s+[A-Z]\w{2,})");
            var initialsOrForeignName = initialsOrForeignNames.Match(slTransaction.Comment).Value;
            if (!string.IsNullOrEmpty(initialsOrForeignName))
            {
                fullName ??= initialsOrForeignName;
            }


            return fullName;
        }
    }
}
