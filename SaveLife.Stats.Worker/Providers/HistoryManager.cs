using Microsoft.Extensions.Options;
using SaveLife.Stats.Domain.Extensions;
using SaveLife.Stats.Worker.Models;
using System.Text.Json;

namespace SaveLife.Stats.Worker.Providers
{
    public class HistoryManager
    {
        private readonly string _currDirectory;
        private const string _projectBasePath = @"..\..\..";
        private readonly LoaderConfig _loaderConfig;
        private IList<RunHistory>? _history;
        public HistoryManager(
            IOptions<LoaderConfig> loaderConfigOptions)
        {
            _currDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _loaderConfig = loaderConfigOptions.Value;
            _history = LoadRunHistory();
        }

        public void SaveRunHistory(RunHistory runHistory)
        {
            var filePath = Path.Combine(_currDirectory, @$"{_projectBasePath}\history.json");
            var runHistoryStr = runHistory.Serialize<RunHistory>();
            File.WriteAllText(filePath, runHistoryStr);
        }

        private IList<RunHistory>? LoadRunHistory()
        {
            var filePath = Path.Combine(_currDirectory, @$"{_projectBasePath}\history.json");
            if (!File.Exists(filePath))
            {
                return null;
            }

            var historyContents = File.ReadAllLines(filePath);
            return historyContents.Select(line => line.Deserialize<RunHistory>()).ToList();
        }

        public SLOriginDataRequest BuildDataRequest()
        {
            var dateTo = _history?.Last().LastTransactionDate;
            dateTo ??= _loaderConfig.LoadToDate;
            int page = _history?.Count == 2 && _history[0].LastTransactionDate == _history[1].LastTransactionDate
                ? _history[1].RequestPage + 1 : 1;

            return new SLOriginDataRequest()
            {
                DateFrom = _loaderConfig.LoadFromDate,
                DateTo = dateTo.Value,
                Page = page
            };

        }
    }
}
