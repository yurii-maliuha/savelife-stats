using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using SaveLife.Stats.Indexer.Constants;
using SaveLife.Stats.Indexer.Models;

namespace SaveLife.Stats.Indexer.Providers
{
    public class ElasticsearchScaffolder
    {
        private readonly ILogger<ElasticsearchScaffolder> _logger;
        private readonly IElasticClient _client;
        private readonly ElasticsearchConfig _elasticsearchConfig;

        public ElasticsearchScaffolder(
            IElasticClient elasticClient,
            IOptions<ElasticsearchConfig> options,
            ILogger<ElasticsearchScaffolder> logger)
        {
            _logger = logger;
            _elasticsearchConfig = options.Value;
            _client = elasticClient;
        }

        public async Task ScaffoldAsync()
        {
            if (_elasticsearchConfig.RunFromScratch)
            {
                await CleanupAsync(_client);
            }

            var indices = await ListIndicesAsync(_client);

            var postfix = DateTime.Now.ToString("yyyyMMdd");

            if (!indices.Contains(ElasticsearchIndexes.TransactionsIndexAliasName))
            {
                var transactionIndexName = $"{ElasticsearchIndexes.TransactionsIndexAliasName.Replace('-', '_')}_{postfix}";
                await CreateIndexAsync(_client, transactionIndexName, $"{ElasticsearchIndexes.TransactionsIndexAliasName}.settings.json");
                await UpdateAliasAsync(_client, transactionIndexName, ElasticsearchIndexes.TransactionsIndexAliasName);
            }
        }

        private async Task UpdateAliasAsync(IElasticClient elasticClient, string indexName, string aliasName)
        {
            var result = await elasticClient.LowLevel.Indices.PutAliasAsync<DynamicResponse>(indexName, aliasName, null);

            if (!result.Success)
            {
                throw new InvalidOperationException($"Error scaffolding Elasticsearch - failed alias {aliasName} creation on {indexName} index.");
            }
        }

        private async Task CreateIndexAsync(IElasticClient elasticClient, string combinedIndexName, string settingsFileName)
        {
            var exists = await elasticClient.Indices.ExistsAsync(combinedIndexName);

            if (exists.Exists)
            {
                throw new InvalidOperationException($"Error scaffolding Elasticsearch - index {combinedIndexName} already exists.");
            }
            else
            {
                var settingsFile = ReadFile(settingsFileName);

                var createResult = await elasticClient.LowLevel.Indices.CreateAsync<DynamicResponse>(combinedIndexName, PostData.String(settingsFile));

                if (!createResult.Success)
                {
                    throw new InvalidOperationException($"Error scaffolding Elasticsearch - error creating {combinedIndexName} index.");
                }
            }
        }

        private static string ReadFile(string filename)
        {
            var path = Path.GetFullPath(Path.Combine("Elasticsearch", filename));

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"File not found needed for Elasticsearch setup operation" +
                    $"at {path}");
            }
            else
            {
                return File.ReadAllText(path);
            }
        }

        private async Task CleanupAsync(IElasticClient elasticClient)
        {
            var indices = await elasticClient.Cat.IndicesAsync(new CatIndicesRequest());
            if (indices.IsValid)
            {
                _logger.LogDebug("Indices:");
                foreach (var index in indices.Records)
                {
                    _logger.LogDebug("   {Index}", index.Index);

                    var deleted = await elasticClient.Indices.DeleteAsync(new DeleteIndexRequest(index.Index));
                    if (deleted.IsValid)
                    {
                        _logger.LogDebug("Deleted index. {Index}", index.Index);
                    }
                }
            }
            else
            {
                _logger.LogDebug("Could not list indices.");
            }
        }

        private async Task<HashSet<string>> ListIndicesAsync(IElasticClient elasticClient)
        {
            var indices = await elasticClient.Cat.AliasesAsync(new CatAliasesRequest());
            if (!indices.IsValid)
            {
                const string message = "Could not list indices.";
                _logger.LogError(message);
                throw new InvalidOperationException(message);
            }

            var allowedAliases = new string[]
            {
                ElasticsearchIndexes.TransactionsIndexAliasName
            };

            return indices.Records
                .Where(r => allowedAliases.Any(allowedAlias => allowedAlias == r.Alias))
                .Select(r => r.Alias)
                .ToHashSet();
        }
    }
}
