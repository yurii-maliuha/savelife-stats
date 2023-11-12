using AutoMapper;
using Microsoft.Extensions.Options;
using SaveLife.Stats.Domain.Extensions;
using SaveLife.Stats.Domain.Models;
using SaveLife.Stats.Downloader.Models;

namespace SaveLife.Stats.Downloader.Providers
{
    public class FileManager
    {
        private readonly IMapper _mapper;
        private IPathResolver _pathResolver;
        private readonly DataSourceConfig _dataSourceConfig;
        public FileManager(
            IMapper mapper,
            IPathResolver pathResolver,
            IOptions<DataSourceConfig> options)
        {
            _mapper = mapper;
            _pathResolver = pathResolver;
            _dataSourceConfig = options.Value;
        }


        public List<long> LoadTransactionsId(DateTime dateFrom)
        {
            string filePath = Path.Combine(_pathResolver.ResolveTransactionsPath(), $"transactions_{dateFrom.Month}-{dateFrom.Year}.json");
            if (!File.Exists(filePath))
            {
                return new List<long>();
            }

            var lines = File.ReadLines(filePath).Take(_dataSourceConfig.BatchSize);
            return lines.Select(x => x.Deserialize<SLOriginTransaction>()).Select(x => x.Id).ToList();
        }

        public async Task SaveTransactions(IList<SLOriginTransaction> originTransactions)
        {
            var transactions = _mapper.Map<List<SLTransaction>>(originTransactions);
            var transactionsStr = transactions.Select(transaction => transaction.Serialize());

            var lastItemDate = originTransactions.Last().Date;
            string filePath = Path.Combine(_pathResolver.ResolveTransactionsPath(), $"transactions_{lastItemDate.Month}-{lastItemDate.Year}.json");
            await File.AppendAllLinesAsync(filePath, transactionsStr);
        }
    }
}
