using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveLife.Stats.Worker.Models;
using SaveLife.Stats.Worker.Providers;

namespace SaveLife.Stats.Worker
{
    public class Loader : BackgroundService
    {
        private readonly ILogger<Loader> _logger;
        private readonly SaveLifeDataThrottler _saveLifeDataProvider;
        private readonly FileManager _fileManager;
        private readonly HistoryManager _historyManager;
        private readonly LoaderConfig _loaderConfig;
        private readonly DataSourceConfig _dataSourceConfig;
        private readonly IHostApplicationLifetime _applicationLifetime;

        public Loader(
            SaveLifeDataThrottler saveLifeDataProvider,
            IOptions<LoaderConfig> loaderConfigOptions,
            IOptions<DataSourceConfig> dataSourceConfigOptions,
            FileManager fileManager,
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
            List<long> edgeTransactionIds = null;

            do
            {
                _logger.LogInformation($"{Environment.NewLine}[{DateTime.Now}]: Itteration {iterattion}");

                edgeTransactionIds ??= _fileManager.LoadTransactionsId(_loaderConfig.LoadFromDate);
                var history = _historyManager.LoadRunHistory();

                var dataRequest = _historyManager.BuildDataRequest(history);
                var response = await _saveLifeDataProvider.LoadDataAsync(dataRequest, stoppingToken);
                var uniqueueTransactions = response.Transactions.Where(x => !edgeTransactionIds.Contains(x.Id)).ToList();
                if (!uniqueueTransactions.Any())
                {
                    _logger.LogWarning($"[{DateTime.Now}]: Empty trnsaction list was returned. Stopping execution.");
                    break;
                }

                await _fileManager.SaveTransactions(uniqueueTransactions);

                var lastItem = response.Transactions.Last();
                history ??= new List<RunHistory>();
                history.Add(new RunHistory()
                {
                    LastTransactionDate = lastItem.Date,
                    LastTransactionId = lastItem.Id,
                    RequestPage = dataRequest.Page
                });

                history = history.Count > 2 ? history.Skip(1).ToList() : history;
                _historyManager.SaveRunHistory(history);

                edgeTransactionIds = response.Transactions.Where(x => x.Date == lastItem.Date).Select(x => x.Id).ToList();

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
