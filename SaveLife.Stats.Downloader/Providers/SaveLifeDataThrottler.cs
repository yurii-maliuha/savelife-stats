using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using SaveLife.Stats.Downloader.Exceptions;
using SaveLife.Stats.Downloader.Models;
using System.Diagnostics;

namespace SaveLife.Stats.Downloader.Providers
{
    public class SaveLifeDataThrottler
    {
        private readonly ISaveLifeDataProvider _dataProvider;
        private readonly LoaderConfig _loaderConfig;
        private readonly ILogger<SaveLifeDataThrottler> _logger;
        private double _previousOperationTime = 0;

        public SaveLifeDataThrottler(
            ISaveLifeDataProvider saveLifeDataProvider,
            IOptions<LoaderConfig> loaderConfigOptions,
            ILogger<SaveLifeDataThrottler> logger)
        {
            _dataProvider = saveLifeDataProvider;
            _loaderConfig = loaderConfigOptions.Value;
            _logger = logger;
        }

        public async Task<SLOriginResponse> LoadDataAsync(SLOriginDataRequest request, CancellationToken cancellationToken)
        {
            var serviceOverwhelmedPolicy = Policy
                .Handle<ServiceOverwhelmedException>()
                .WaitAndRetryAsync(new[] {
                    TimeSpan.FromSeconds(_loaderConfig.ThrottleSeconds),
                    TimeSpan.FromSeconds(_loaderConfig.ThrottleSeconds * 2),
                    TimeSpan.FromSeconds(_loaderConfig.ThrottleSeconds * 3)
                }, (exception, timeSpan, context) =>
                {
                    _logger.LogWarning($"[{DateTime.Now}]:{nameof(LoadDataAsync)} has failed with ServiceOverwhelmedException. Retrying.");
                });

            var result = await serviceOverwhelmedPolicy.ExecuteAsync(() => RetrieveDataAsync(request, cancellationToken));

            return result;
        }

        private async Task<SLOriginResponse> RetrieveDataAsync(SLOriginDataRequest request, CancellationToken cancellationToken)
        {
            if (_previousOperationTime > _loaderConfig.MaxSeccondsPerOperation)
            {
                _previousOperationTime = 0;
                throw new ServiceOverwhelmedException($"Previous operation execution tool {_previousOperationTime} whereas {_loaderConfig.MaxSeccondsPerOperation} was expected");
            }

            var timer = new Stopwatch();
            timer.Start();
            var result = await _dataProvider.LoadDataAsync(request, cancellationToken);
            timer.Stop();

            _previousOperationTime = timer.Elapsed.TotalSeconds;

            await Task.Delay(TimeSpan.FromSeconds(_loaderConfig.ThrottleSeconds));

            return result;
        }
    }
}
