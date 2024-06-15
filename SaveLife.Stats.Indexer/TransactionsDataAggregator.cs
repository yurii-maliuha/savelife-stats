using AutoMapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nest;
using SaveLife.Stats.Domain.Models;
using SaveLife.Stats.Indexer.Providers;

namespace SaveLife.Stats.Indexer
{
    public class TransactionsDataAggregator : BackgroundService
    {
        private readonly ILogger<TransactionsDataAggregator> _logger;
        private readonly ElasticsearchProvider _searchProvider;
        private readonly MongoDbProvider _mongoDbProvider;
        private readonly MD5HashProvider _hashProvider;
        private readonly IMapper _mapper;

        public TransactionsDataAggregator(
            ElasticsearchProvider elasticsearchProvider,
            MongoDbProvider mongoDbProvider,
            MD5HashProvider md5HashProvider,
            IMapper mapper,
            ILogger<TransactionsDataAggregator> logger)
        {
            _searchProvider = elasticsearchProvider;
            _mongoDbProvider = mongoDbProvider;
            _hashProvider = md5HashProvider;
            _mapper = mapper;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogWarning($"Starting {nameof(TransactionsDataAggregator)}");
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

                    if(donatorEntities?.Any() ?? false)
                    {
                        await _mongoDbProvider.UpsertDonatorsAsync(donatorEntities!);
                        _logger.LogInformation($"[*] Update identities from [{donatorEntities.First().Identity}; {donatorEntities.Last().Identity}]");
                    }


                    //var filePathToCardholders = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"..\..\..\Data\benefactors.json");
                    //var benefactorsStr = benefactors.Select(identity => identity.Serialize());
                    //await File.AppendAllLinesAsync(filePathToCardholders, benefactorsStr);


                    afterKey = key;
                } while (afterKey != null);


                _logger.LogWarning($"Finishing {nameof(TransactionsDataAggregator)}");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
    }
}
