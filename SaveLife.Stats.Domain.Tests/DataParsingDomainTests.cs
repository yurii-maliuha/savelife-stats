using FluentAssertions;
using SaveLife.Stats.Domain.Domains;

namespace SaveLife.Stats.Domain.Tests
{
    [TestClass]
    public class DataParsingDomainTests
    {
        [TestMethod]
        [DataRow("UKR ***6976 (100 UAH)", "***6976")]
        [DataRow("null ***1841 (1 UAH)", "***1841")]
        [DataRow("SGP ***6808 (119 EUR)", "***6808")]
        public void TryParseIdentity_ReturnsCardNumber(string input, string parsedIdentity)
        {
            var parser = new DataParsingDomain();

            var identity = parser.TryParseIdentity(new Models.SLTransaction() { Comment = input });

            identity.FullName.Should().BeNull();
            identity.LegalName.Should().BeNull();
            identity.CardNumber.Should().Be(parsedIdentity);
        }

        [TestMethod]
        [DataRow("Вознюк-Вознюк Мар'яна Ярославівна -- благодійність", "вознюк-вознюк мар'яна")]
        [DataRow("Платежи Битлз_бездоговорные -- Благодійна допомога українцям від Медицький Руслан", "медицький руслан")]
        [DataRow("Транзит за розрах Digital Platform -- Благодiйний Внесок ДОПОМОГУ ЗСУ, Процишин Богдан", "процишин богдан")]
        [DataRow("K3_транз.рахунок платежi BP 3853335 -- Благодiйна допомога вiд Iвакiн В\u0027ячеслав Володимирович, Iвакiн В\u0027ячеслав Володимирович", "iвакiн в'ячеслав")] // ignore middle name?
        [DataRow("Julia Chystoserdova -- SLAVA UKRAINI 4F (215 PLN)", "julia chystoserdova")]
        [DataRow("Mr Oleksiy -- (480 USD)", "mr oleksiy")]
        [DataRow("ШКIЦЬКIЙ В.В. ФОП -- Благодiйна допомога по програмi \u0022Фонд", "шкiцькiй в.в.")]
        [DataRow("ШКIЦЬКIЙ В.В ФОП -- Благодiйна допомога по програмi \u0022Фонд", "шкiцькiй в.в")]
        [DataRow("ШКIЦЬКIЙ В. ФОП -- Благодiйна допомога по програмi \u0022Фонд", "шкiцькiй в.ф")]
        [DataRow("K3_транз.рахунок платежi BP 3853335 -- Благодiйна допомога вiд Мукумов Уктам", "мукумов уктам")]
        [DataRow("Благодiйна допомога вiйськовослужбовцям Платник Мидлик IГОРОВИЧ", "мидлик iгорович")]

        //[DataRow("Благодійна допомога військовослужбовця., Зищук Олесандр ЗАВОДСЬКА 0-00 МІСТО", "Зищук Олександр")] // misspelled names are ignored for now
        public void TryParseIdentity_ReturnsFullName(string input, string parsedIdentity)
        {
            var parser = new DataParsingDomain();

            var identity = parser.TryParseIdentity(new Models.SLTransaction() { Comment = input });

            identity.CardNumber.Should().BeNull();
            identity.LegalName.Should().BeNull();
            identity.FullName.Should().Be(parsedIdentity);
            identity.FullName.Should().Be(identity.Id);
        }

        [TestMethod]
        [Ignore("Should be fixed later")]
        [DataRow("T5 Team -- GIFT OR DONATION CHARITABLE DONATIO N TO UKRAINIAN MILITARY (1000 USD)")]
        public void TryParseIdentity_ReturnsUnidentified(string input)
        {
            var parser = new DataParsingDomain();

            var identity = parser.TryParseIdentity(new Models.SLTransaction() { Comment = input });

            identity.FullName.Should().BeNull();
            identity.LegalName.Should().BeNull();
            identity.CardNumber.Should().BeNull();
            identity.Id.Should().Be("Unidentified");
        }
    }
}
