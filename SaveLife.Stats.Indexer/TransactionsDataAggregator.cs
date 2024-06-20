using AutoMapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using SaveLife.Stats.Domain.Extensions;
using SaveLife.Stats.Domain.Models;
using SaveLife.Stats.Indexer.Models;
using SaveLife.Stats.Indexer.Providers;
using System.Text;

namespace SaveLife.Stats.Indexer
{
    public class TransactionsDataAggregator : BackgroundService
    {
        private readonly ILogger<TransactionsDataAggregator> _logger;
        private readonly ElasticsearchProvider _searchProvider;
        private readonly MongoDbProvider _mongoDbProvider;
        private readonly MD5HashProvider _hashProvider;
        private readonly AggregatorConfig _config;
        private readonly IMapper _mapper;

        public TransactionsDataAggregator(
            ElasticsearchProvider elasticsearchProvider,
            MongoDbProvider mongoDbProvider,
            MD5HashProvider md5HashProvider,
            IMapper mapper,
            IOptions<AggregatorConfig> configOptions,
            ILogger<TransactionsDataAggregator> logger)
        {
            _searchProvider = elasticsearchProvider;
            _mongoDbProvider = mongoDbProvider;
            _hashProvider = md5HashProvider;
            _mapper = mapper;
            _config = configOptions.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogWarning($"Starting {nameof(TransactionsDataAggregator)}");
                
                if(_config.Enable)
                {
                    await UpdateDonatorsAggregationAsync();
                }

                var topDonators = await _mongoDbProvider.GetTopDonaterAsync(50);
                var filePathToCardholders = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"..\..\..\Data\top-donators.json");
                var benefactorsStr = topDonators.Select(x => new { x.Identity, x.TotalDonation, x.TransactionsCount }.Serialize());
                await File.WriteAllLinesAsync(filePathToCardholders, benefactorsStr, Encoding.UTF8);

                _logger.LogWarning($"Finishing {nameof(TransactionsDataAggregator)}");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task UpdateDonatorsAggregationAsync()
        {
            CompositeKey? afterKey = null;
            do
            {
                var (donators, key) = await _searchProvider.AggregateDonators(afterKey);
                var donatorEntities = donators.Select(donator =>
                {
                    var entity = _mapper.Map<DonatorEntity>(donator);
                    entity.Id = _hashProvider.ComputeHash(donator.Identity);
                    return entity;
                });

                if (donatorEntities?.Any() ?? false)
                {
                    await _mongoDbProvider.UpsertDonatorsAsync(donatorEntities!);
                    _logger.LogInformation($"[*] Update identities from [{donatorEntities.First().Identity}; {donatorEntities.Last().Identity}]");
                }

                afterKey = key;
            } while (afterKey != null);
        }
    }
}
