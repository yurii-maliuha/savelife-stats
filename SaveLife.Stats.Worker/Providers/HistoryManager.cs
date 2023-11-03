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

        public HistoryManager(
            IOptions<LoaderConfig> loaderConfigOptions,
            IPathResolver pathResolver)
        {
            _loaderConfig = loaderConfigOptions.Value;
            _pathResolver = pathResolver;
        }

        public void SaveRunHistory(IList<RunHistory> nextHistory)
        {
            var nextHistoryStr = nextHistory.Select(x => x.Serialize());

            var filePath = Path.Combine(_pathResolver.ResolveHistoryPath(), "history.json");
            File.WriteAllLinesAsync(filePath, nextHistoryStr);
        }

        public IList<RunHistory>? LoadRunHistory()
        {
            var filePath = Path.Combine(_pathResolver.ResolveHistoryPath(), "history.json");
            if (!File.Exists(filePath))
            {
                return null;
            }

            var historyContents = File.ReadAllLines(filePath);
            return historyContents.Select(line => line.Deserialize<RunHistory>()).ToList();
        }

        public SLOriginDataRequest BuildDataRequest(IList<RunHistory>? history)
        {
            var dateTo = history?.Last().LastTransactionDate;
            dateTo ??= _loaderConfig.LoadToDate;
            int page = history?.Count == 2 && history[0].LastTransactionDate == history[1].LastTransactionDate
                ? history[1].RequestPage + 1 : 1;

            return new SLOriginDataRequest()
            {
                DateFrom = _loaderConfig.LoadFromDate,
                DateTo = dateTo.Value,
                Page = page
            };

        }
    }
}
