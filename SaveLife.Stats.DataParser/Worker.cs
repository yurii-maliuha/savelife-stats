using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaveLife.Stats.DataParser.Provider;
using SaveLife.Stats.Domain.Domains;
using SaveLife.Stats.Domain.Models;
using System.Collections.Concurrent;

namespace SaveLife.Stats.DataParser
{
    public class Worker : BackgroundService
    {
        private const int COLLECTION_CAPACITY = 5000;
        private readonly ILogger<Worker> _logger;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly DataParsingDomain _dataParsingDomain;
        public Worker(
            IHostApplicationLifetime applicationLifetime,
            DataParsingDomain dataParsingDomain,
            ILogger<Worker> logger)
        {
            _applicationLifetime = applicationLifetime;
            _dataParsingDomain = dataParsingDomain;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var transactionsBlockingCollection = new BlockingCollection<SLTransaction>(COLLECTION_CAPACITY);
            var dataPablisher = new DataPublisher(transactionsBlockingCollection, _logger);
            var dataConsumer = new DataConsumer(transactionsBlockingCollection, _dataParsingDomain, _logger);

            try
            {
                await Task.WhenAll(dataPablisher.PublishData(), dataConsumer.ConsumeData());

                _logger.LogInformation($"Finished at {DateTime.Now}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                await dataConsumer.SaveIdenities();
                _applicationLifetime.StopApplication();
            }
        }
    }
}
