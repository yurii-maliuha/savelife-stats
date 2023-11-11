using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaveLife.Stats.Domain.Extensions;
using SaveLife.Stats.Domain.Models;
using SaveLife.Stats.Worker.Tests.Stubs;

namespace SaveLife.Stats.Worker.Tests
{
    [TestClass]
    public class DataTests
    {
        [TestMethod]
        public void Test1()
        {
            var path = @$"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\..\..\SaveLife.Stats.Data\Raw";

            var currTransactionsStr = File.ReadAllLines(@$"{path}\transactions_1-2023.json");
            currTransactionsStr.Should().NotBeNull();

            var currTransactions = currTransactionsStr
                .Select(x => x.Deserialize<SLTransaction>())
                .ToList();

            var v2TransactionsStr = File.ReadAllLines(@$"{path}\v2_transactions_1-2023.json");
            v2TransactionsStr.Should().NotBeNull();

            var v2Transactions = v2TransactionsStr
                .Select(x => x.Deserialize<SLTransaction>())
                .ToList();

            var v3TransactionsStr = File.ReadAllLines(@$"{path}\v3_transactions_1-2023.json");
            v3TransactionsStr.Should().NotBeNull();

            var v3Transactions = v3TransactionsStr
                .Select(x => x.Deserialize<SLTransaction>())
                .Where(x => x.Date >= v2Transactions.Last().Date && x.Date <= currTransactions.First().Date)
                .ToList();

            v2Transactions = v2Transactions
                .Where(x => x.Date <= v3Transactions.First().Date && x.Date <= currTransactions.First().Date)
                .ToList();



            var v2Ids = v2Transactions.Select(x => x.Id).ToList();
            var v3Ids = v3Transactions.Select(x => x.Id).ToList();
            var currIds = currTransactions.Select(x => x.Id).ToList();

            var v2Only = v2Transactions.Where(x => !currIds.Contains(x.Id) && !v3Ids.Contains(x.Id)).ToList();
            var v3Only = v3Transactions.Where(x => !currIds.Contains(x.Id) && !v2Ids.Contains(x.Id)).ToList();
            var currOnly = currTransactions.Where(x => !v3Ids.Contains(x.Id) && !v2Ids.Contains(x.Id)).ToList();

            currOnly.Count.Should().BeGreaterThan(1);
        }


        [TestMethod]
        public void Test2()
        {
            var path = @$"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\..\..\SaveLife.Stats.Data\Raw";

            var currTransactionsStr = File.ReadAllLines(@$"{path}\transactions_1-2023.json");
            currTransactionsStr.Should().NotBeNull();

            var currTransactions = currTransactionsStr
                .Select(x => x.Deserialize<SLTransaction>())
                .ToList();

            currTransactions = currTransactions.OrderByDescending(x => x.Date).ThenBy(x => x.Id).ToList();

            var v3TransactionsStr = File.ReadAllLines(@$"{path}\v3_1_transactions_1-2023.json");
            v3TransactionsStr.Should().NotBeNull();

            var v3Transactions = v3TransactionsStr
                .Select(x => x.Deserialize<SLTransaction>())
                .ToList();

            v3Transactions = v3Transactions.OrderByDescending(x => x.Date).ThenBy(x => x.Id).ToList();

            var outputPath = new PathResolverStub().ResolveTransactionsPath();
            File.WriteAllLines(@$"{outputPath}\v3_ordered_transactions_1-2023.json", v3Transactions.Select(x => x.Serialize()));
            File.WriteAllLines(@$"{outputPath}\ordered_transactions_1-2023.json", currTransactions.Select(x => x.Serialize()));




            v3Transactions.Count.Should().BeGreaterThan(1);
        }
    }
}
