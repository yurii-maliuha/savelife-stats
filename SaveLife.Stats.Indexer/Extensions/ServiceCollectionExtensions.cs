using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nest;
using SaveLife.Stats.Domain.Models;
using SaveLife.Stats.Indexer.Constants;
using SaveLife.Stats.Indexer.Models;
using System.Diagnostics;
using System.Text;

namespace SaveLife.Stats.Indexer.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElasticSearchProviders(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ElasticsearchConfig>(configuration.GetSection(ElasticsearchConfig.DisplayName));
            services.AddSingleton<IElasticClient>(sp =>
            {
                var elasticSearchConfig = sp.GetService<IOptions<ElasticsearchConfig>>().Value;


                var connectSettings = new ConnectionSettings(
                    new SingleNodeConnectionPool(new Uri(elasticSearchConfig.Uri)))
                    .DefaultMappingFor<Transaction>(m => m.IndexName(ElasticsearchIndexes.TransactionsIndexAliasName));

                connectSettings
                        // Turn on debug mode to get the full stack trace on elasticsearch errors
                        // https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/configuration-options.html
                        //.EnableDebugMode()
                        // Force all requests to have ?pretty=true appended
                        .PrettyJson()
                        //Allows the client to log the response bytes before deserialization.
                        .DisableDirectStreaming()
                        // Setup logging
                        .OnRequestCompleted(LogDebugDetails);

                void LogDebugDetails(IApiCallDetails response)
                {
                    var requestBody = response.RequestBodyInBytes != null
                        ? $"\n{Encoding.UTF8.GetString(response.RequestBodyInBytes)}"
                        : "";

                    if (!response.Success)
                    {
                        Debug.WriteLine($"Elasticsearch error. {response.HttpStatusCode} {response.DebugInformation}" +
                                     $"\n{requestBody}",
                           response.OriginalException);
                    }
                }

                return new ElasticClient(connectSettings);
            });

            return services;
        }
    }
}
