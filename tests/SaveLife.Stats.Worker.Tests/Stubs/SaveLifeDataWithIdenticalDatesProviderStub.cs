using AutoFixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SaveLife.Stats.Worker.Models;
using SaveLife.Stats.Worker.Providers;

namespace SaveLife.Stats.Worker.Tests.Stubs
{
    public class SaveLifeDataWithIdenticalDatesProviderStub : ISaveLifeDataProvider
    {
        private readonly DataSourceConfig _dataSourceConfig;
        private readonly LoaderConfig _loaderConfig;
        private IList<SLOriginTransaction> _transactions;

        public SaveLifeDataWithIdenticalDatesProviderStub(
            IServiceProvider serviceProvider,
            int transactionsCount)
        {
            _dataSourceConfig = serviceProvider.GetRequiredService<IOptions<DataSourceConfig>>().Value;
            _loaderConfig = serviceProvider.GetRequiredService<IOptions<LoaderConfig>>().Value;
            _transactions = BuildTransactionsList(transactionsCount);
        }

        private IList<SLOriginTransaction> BuildTransactionsList(int transactionsCount)
        {
            var fixture = new Fixture();
            var timeDiffStep = (_loaderConfig.LoadToDate - _loaderConfig.LoadFromDate).TotalMilliseconds / transactionsCount;
            var transactions = fixture.CreateMany<SLOriginTransaction>(transactionsCount).ToList();
            double? theSameTimeStep = null;
            for (var i = 0; i < transactions.Count; i++)
            {
                var iteration = i / _dataSourceConfig.BatchSize;
                if (iteration >= 3 && iteration <= 5)
                {
                    theSameTimeStep ??= iteration * _dataSourceConfig.BatchSize + i * timeDiffStep;
                }
                double timeDiff = iteration >= 3 && iteration <= 5
                    ? theSameTimeStep!.Value
                    : iteration * _dataSourceConfig.BatchSize + i * timeDiffStep;
                var newDate = _loaderConfig.LoadToDate.AddMilliseconds(-timeDiff);
                transactions[i].Date = new DateTime(newDate.Year, newDate.Month, newDate.Day, newDate.Hour, newDate.Minute, newDate.Second);
            }

            return transactions;
        }

        public Task<SLOriginResponse> LoadDataAsync(SLOriginDataRequest request, CancellationToken cancellationToken)
        {
            var iterationTransactions = _transactions
                .Where(x => x.Date >= request.DateFrom && x.Date <= request.DateTo)
                .OrderByDescending(x => x.Date)
                .Skip((request.Page - 1) * _dataSourceConfig.BatchSize)
                .Take(_dataSourceConfig.BatchSize)
                .ToList();

            return Task.FromResult(new SLOriginResponse()
            {
                TotalCount = _transactions.Count,
                Transactions = iterationTransactions
            });
        }
    }
}
