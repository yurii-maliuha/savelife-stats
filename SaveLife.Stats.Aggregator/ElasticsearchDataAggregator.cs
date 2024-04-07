using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SaveLife.Stats.Aggregator
{
    public class ElasticsearchDataAggregator : BackgroundService
    {
        private readonly ILogger<ElasticsearchDataAggregator> _logger;

        public ElasticsearchDataAggregator(
            ILogger<ElasticsearchDataAggregator> logger)
        {
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogWarning($"Starting {nameof(ElasticsearchDataAggregator)}");

            return Task.CompletedTask;
        }
    }
}
