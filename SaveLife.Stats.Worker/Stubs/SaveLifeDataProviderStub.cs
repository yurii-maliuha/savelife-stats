using AutoFixture;
using Microsoft.Extensions.Options;
using SaveLife.Stats.Worker.Models;
using SaveLife.Stats.Worker.Providers;

namespace SaveLife.Stats.Worker.Stubs
{
    public class SaveLifeDataProviderStub : ISaveLifeDataProvider
    {
        private readonly Fixture _fixture;
        private readonly DataSourceConfig _dataSourceConfig;
        private int _itteration = 0;
        private const int TOTAL_ITERATIONS_COUNT = 1000;

        public SaveLifeDataProviderStub(
            IOptions<DataSourceConfig> options)
        {
            _fixture = new Fixture();
            _dataSourceConfig = options.Value;
        }

        public Task<SLOriginResponse> LoadDataAsync(SLOriginDataRequest request, CancellationToken cancellationToken)
        {
            return GeneralScenario(request, cancellationToken);
        }

        private Task<SLOriginResponse> GeneralScenario(SLOriginDataRequest request, CancellationToken cancellationToken)
        {
            var timeDiffStep = (request.DateTo - request.DateFrom).TotalMilliseconds / (TOTAL_ITERATIONS_COUNT * _dataSourceConfig.BatchSize);
            var transactions = _fixture.CreateMany<SLOriginTransaction>(_dataSourceConfig.BatchSize).ToList();
            for (var i = 0; i < transactions.Count; i++)
            {
                var timeDiff = _itteration * _dataSourceConfig.BatchSize + i * timeDiffStep;
                transactions[i].Date = request.DateTo;
                transactions[i].Date = transactions[i].Date.AddMilliseconds(-timeDiff);
                if (transactions[i].Date.Hour == 22)
                {
                    // imitate same time transactions
                    transactions[i].Date = new DateTime(transactions[i].Date.Year, transactions[i].Date.Month, transactions[i].Date.Day, transactions[i].Date.Hour, 7, 0);
                }
            }

            ++_itteration;

            return Task.FromResult(new SLOriginResponse()
            {
                TotalCount = TOTAL_ITERATIONS_COUNT * _dataSourceConfig.BatchSize,
                Transactions = transactions
            });
        }
    }
}
