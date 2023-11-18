using SaveLife.Stats.Domain.Extensions;
using SaveLife.Stats.Domain.Models;

namespace SaveLife.Stats.Indexer.Providers
{
    public class TransactionReader
    {
        public IList<SLTransaction> ReadTransactions(int skip, int take)
        {
            string filePath = Path.Combine(@$"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\..\SaveLife.Stats.Data\Raw", $"transactions_1-2023.json");
            if (!File.Exists(filePath))
            {
                return new List<SLTransaction>();
            }

            var lines = File.ReadLines(filePath).Skip(skip).Take(take);
            return lines.Select(x => x.Deserialize<SLTransaction>()).ToList();
        }
    }
}
