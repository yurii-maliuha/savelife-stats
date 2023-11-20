using SaveLife.Stats.Domain.Extensions;
using SaveLife.Stats.Domain.Models;
using System.Text.RegularExpressions;

namespace SaveLife.Stats.Domain.Domains
{
    public class DataParsingDomain
    {
        private readonly HashSet<string> _knownNames;
        public DataParsingDomain()
        {
            _knownNames = LoadKnownNames();
        }

        public string? TryParseCardNumber(SLTransaction slTransaction)
        {
            Regex rx = new Regex(@"\*\*\*\d{4}");
            MatchCollection matches = rx.Matches(slTransaction.Comment);
            return matches.FirstOrDefault()?.Value;
        }

        public string? TryParseFullName(SLTransaction slTransaction)
        {
            string? fullName = null;
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

        public string? TryParseLegalName(SLTransaction slTransaction)
        {
            if (slTransaction.Comment.StartsWith("ТзОВ"))
            {
                var sd = 12;
            }
            Regex legalEntityPatern = new Regex(@"(ПрАТ|ТОВ|ОСББ|ТзОВ|ФГ|ПП)\s+""\s*[IІЖЄЇА-Я\-'iіжєїa-я0-9 ]+\s*""");
            var legalName = legalEntityPatern.Matches(slTransaction.Comment).LastOrDefault()?.Value.Replace(@"""", "'");
            return legalName;
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
    }
}
