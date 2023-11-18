using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveLife.Stats.Downloader.Models;
using SaveLife.Stats.Downloader.Providers;

namespace SaveLife.Stats.Downloader
{
    public class Loader : BackgroundService
    {
        private readonly ILogger<Loader> _logger;
        private readonly SaveLifeDataThrottler _saveLifeDataProvider;
        private readonly TransactionManager _fileManager;
        private readonly HistoryManager _historyManager;
        private readonly LoaderConfig _loaderConfig;
        private readonly DataSourceConfig _dataSourceConfig;
        private readonly IHostApplicationLifetime _applicationLifetime;

        public Loader(
            SaveLifeDataThrottler saveLifeDataProvider,
            IOptions<LoaderConfig> loaderConfigOptions,
            IOptions<DataSourceConfig> dataSourceConfigOptions,
            TransactionManager fileManager,
            HistoryManager historyManager,
            IHostApplicationLifetime applicationLifetime,
            ILogger<Loader> loader)
        {
            _saveLifeDataProvider = saveLifeDataProvider;
            _loaderConfig = loaderConfigOptions.Value;
            _dataSourceConfig = dataSourceConfigOptions.Value;
            _fileManager = fileManager;
            _historyManager = historyManager;
            _applicationLifetime = applicationLifetime;
            _logger = loader;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Started loading at {DateTime.Now}");
            int iterattion = 1;
            List<EdgeTransaction> edgeTransactions = new List<EdgeTransaction>();

            do
            {
                _logger.LogInformation($"{Environment.NewLine}[{DateTime.Now}]: Itteration {iterattion}");

                var dataRequest = _historyManager.BuildDataRequest();
                var response = await _saveLifeDataProvider.LoadDataAsync(dataRequest, stoppingToken);
                var uniqueueTransactions = response.Transactions.Where(x => !edgeTransactions.Select(x => x.Id).Contains(x.Id)).ToList();
                if (!uniqueueTransactions.Any())
                {
                    _logger.LogWarning($"[{DateTime.Now}]: Empty trnsaction list was returned. Stopping execution.");
                    break;
                }

                await _fileManager.SaveTransactions(uniqueueTransactions);

                var lastItem = response.Transactions.Last();
                await _historyManager.SaveRunHistory(lastItem.Id);

                edgeTransactions = edgeTransactions
                    .Where(x => x.Date == lastItem.Date)
                    .Concat(
                        response.Transactions
                        .Where(x => x.Date == lastItem.Date)
                        .Select(x => new EdgeTransaction() { Id = x.Id, Date = x.Date }))
                   .ToList();

                ++iterattion;
                _logger.LogInformation($"[{DateTime.Now}]: Response total count: {response.TotalCount}");

                if (uniqueueTransactions.Count != _dataSourceConfig.BatchSize)
                {
                    _logger.LogInformation($"[{DateTime.Now}]: {_dataSourceConfig.BatchSize - uniqueueTransactions.Count} duplicates were filtered out");
                }
            }
            while (!stoppingToken.IsCancellationRequested && iterattion <= _loaderConfig.MaxIterationsCount);

            _logger.LogInformation($"Stopped at {DateTime.Now}");
            _applicationLifetime.StopApplication();

        }
    }
}
