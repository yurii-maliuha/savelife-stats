using SaveLife.Stats.Domain.Extensions;
using SaveLife.Stats.Domain.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SaveLife.Stats.Domain.Domains
{
    public class DataParsingDomain
    {
        private readonly CultureInfo _culture;
        private readonly HashSet<string> _knownNames;
        public DataParsingDomain()  
        {
            _knownNames = LoadKnownNames();
            _culture = new CultureInfo("uk", false);
        }

        public Identity TryParseIdentity(SLTransaction slTransaction)
        {
            var(cardNumber, fullName, legalName) = (TryParseCardNumber(slTransaction), TryParseFullName(slTransaction), TryParseLegalName(slTransaction));
            return new Identity()
            {
                Id = cardNumber ?? legalName ?? fullName ?? "Unidentified",
                CardNumber = cardNumber,
                FullName = legalName == null ? fullName : null,
                LegalName = legalName
            };
        }

        private string? TryParseCardNumber(SLTransaction slTransaction)
        {
            Regex rx = new Regex(@"\*\*\*\d{4}");
            MatchCollection matches = rx.Matches(slTransaction.Comment);
            return matches.FirstOrDefault()?.Value;
        }

        private string? TryParseFullName(SLTransaction slTransaction)
        {
            string? fullName = null;
            // Прізвище Ім'я
            // web format: ([IІ\x{0400}-\x{042F}][\-IiІі'\x{0400}-\x{04FF}]+\s[IІ\x{0400}-\x{042F}][\-IiІі'\x{0400}-\x{04FF}]+)
            Regex generalFullNamePattern = new Regex(@"([IІ\u0400-\u042F][\-IiІі'\u0400-\u04FF]+\s[IІ\u0400-\u042F][\-IiІі'\u0400-\u04FF]+)");
            var fullNameMatches = generalFullNamePattern.Matches(slTransaction.Comment).Select(x => x.Groups[0].Value);
            foreach (var possiblyFullName in fullNameMatches)
            {
                fullName ??= possiblyFullName.Split(' ').Any(x => _knownNames.Contains(x.ToLower(_culture))) == true ? possiblyFullName.ToLower(_culture) : null;
            }

            // від Ім'я Прізвище || Платник Ім'я Прізвище
            Regex fromFullNamePattern = new Regex(@"((?<=(від|вiд|Платник)\s)([IІ\u0400-\u042F][\-IiІі'\u0400-\u04FF]+\s[IІ\u0400-\u042F][\-IiІі'\u0400-\u04FF]+))");
            var fromFullNameMatches = fromFullNamePattern.Matches(slTransaction.Comment).Select(x => x.Groups[0].Value);
            fullName ??= fromFullNameMatches.Where(x => x.ToLower(_culture) != "повернись живим").FirstOrDefault()?.ToLower(_culture);

            // Прізвище І. C. || Mr Lastname || FirstName LastName
            // web format: ([IІ\x{0400}-\x{042F}][\-IiІі'\x{0400}-\x{04FF}]+\s+[IІ\x{0400}-\x{042F}]{1}\.\s*([IІ\x{0400}-\x{042F}]{1}(\.*)){0,1})
            Regex initialsOrForeignNames = new Regex(@"([IІ\u0400-\u042F][\-IiІі'\u0400-\u04FF]+\s+[IІ\u0400-\u042F]\.\s*([IІ\u0400-\u042F]\.{0,1}){0,1})|([mrsMRS]{2}\s+[A-Z]\w{2,})|([A-Z]\w{2,}\s+[A-Z]\w{2,})");
            var initialsOrForeignName = initialsOrForeignNames.Match(slTransaction.Comment).Value;
            if (!string.IsNullOrEmpty(initialsOrForeignName))
            {
                fullName ??= initialsOrForeignName.ToLower(_culture);
            }


            return fullName;
        }

        private string? TryParseLegalName(SLTransaction slTransaction)
        {
            Regex legalEntityPatern = new Regex(@"(ПрАТ|ТОВ|ОСББ|ТзОВ|ФГ|ПП)\s+""\s*[IІЖЄЇА-Я\-'iіжєїa-я0-9 ]+\s*""");
            var legalName = legalEntityPatern.Matches(slTransaction.Comment).LastOrDefault()?.Value.Replace(@"""", "'")?.ToLower(_culture);
            var notChars = legalName?.Where(x => !char.IsLetter(x));

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
