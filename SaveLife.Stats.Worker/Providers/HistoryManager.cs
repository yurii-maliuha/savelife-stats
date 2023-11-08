using Microsoft.Extensions.Options;
using SaveLife.Stats.Domain.Extensions;
using SaveLife.Stats.Worker.Models;
using System.Text.Json;

namespace SaveLife.Stats.Worker.Providers
{
    public class HistoryManager
    {
        private readonly LoaderConfig _loaderConfig;
        private IPathResolver _pathResolver;
        private RunHistory _history;

        public HistoryManager(
            IOptions<LoaderConfig> loaderConfigOptions,
            IPathResolver pathResolver)
        {
            _loaderConfig = loaderConfigOptions.Value;
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
                    LastTransactionId = null
                };
            }

            var historyContent = File.ReadAllText(filePath);
            return historyContent.Deserialize<RunHistory>();
        }
    }
}
