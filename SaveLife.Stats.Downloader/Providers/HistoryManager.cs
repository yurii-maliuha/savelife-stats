using Microsoft.Extensions.Options;
using SaveLife.Stats.Domain.Extensions;
using SaveLife.Stats.Downloader.Models;
using System.Text.Json;

namespace SaveLife.Stats.Downloader.Providers
{
    public class HistoryManager
    {
        private readonly LoaderConfig _loaderConfig;
        private readonly DataSourceConfig _dataSourceConfig;
        private IPathResolver _pathResolver;
        private RunHistory _history;

        public HistoryManager(
            IOptions<LoaderConfig> loaderConfigOptions,
            IOptions<DataSourceConfig> dataSourceOptions,
            IPathResolver pathResolver)
        {
            _loaderConfig = loaderConfigOptions.Value;
            _dataSourceConfig = dataSourceOptions.Value;
            _pathResolver = pathResolver;
            _history = LoadRunHistory();
        }

        public async Task SaveRunHistory(long lastTransactionId)
        {
            _history.LastTransactionId = lastTransactionId;

            var filePath = Path.Combine(_pathResolver.ResolveHistoryPath(), "history.json");
            await File.WriteAllTextAsync(filePath, _history.Serialize());
        }

        public SLOriginDataRequest BuildDataRequest()
        {
            var initialIteration = _history.LastTransactionId == null;
            _history.Page = initialIteration ? 1 : _history.Page + 1;
            return new SLOriginDataRequest()
            {
                DateFrom = _history.DateFrom,
                DateTo = _history.DateTo,
                Page = _history.Page
            };

        }

        private RunHistory LoadRunHistory()
        {
            var filePath = Path.Combine(_pathResolver.ResolveHistoryPath(), "history.json");
            if (!File.Exists(filePath))
            {
                return new RunHistory()
                {
                    DateFrom = _loaderConfig.LoadFromDate,
                    DateTo = _loaderConfig.LoadToDate,
                    Page = 1,
                    PerPage = _dataSourceConfig.BatchSize,
                    LastTransactionId = null
                };
            }

            var historyContent = File.ReadAllText(filePath);
            var history = historyContent.Deserialize<RunHistory>();
            if (history.PerPage != _dataSourceConfig.BatchSize)
            {
                throw new ArgumentException("History per page differes from current batch size");
            }

            return history;
        }
    }
}
