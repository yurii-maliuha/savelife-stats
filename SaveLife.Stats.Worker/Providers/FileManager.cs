using AutoMapper;
using SaveLife.Stats.Domain.Extensions;
using SaveLife.Stats.Domain.Models;
using SaveLife.Stats.Worker.Models;

namespace SaveLife.Stats.Worker.Providers
{
    public class FileManager
    {
        private readonly IMapper _mapper;
        private readonly string _currDirectory;
        private const string _projectBasePath = @"..\..\..";
        public FileManager(
            IMapper mapper)
        {
            _mapper = mapper;
            _currDirectory = AppDomain.CurrentDomain.BaseDirectory;
        }
        public async Task SaveRawData(IList<SLOriginTransaction> originTransactions)
        {
            var transactions = _mapper.Map<List<SLTransaction>>(originTransactions);
            var transactionsStr = transactions.Select(transaction => transaction.Serialize());

            var lastItemDate = originTransactions.Last().Date;
            string filePath = Path.Combine(_currDirectory, @$"{_projectBasePath}\..\SaveLife.Stats.Data\Raw\transactions_{lastItemDate.Month}-{lastItemDate.Year}.json");
            await File.AppendAllLinesAsync(filePath, transactionsStr);
        }
    }
}
