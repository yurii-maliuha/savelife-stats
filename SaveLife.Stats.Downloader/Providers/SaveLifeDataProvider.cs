using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveLife.Stats.Domain.Extensions;
using SaveLife.Stats.Downloader.Models;
using System.Text.Json;

namespace SaveLife.Stats.Downloader.Providers
{
    public class SaveLifeDataProvider : ISaveLifeDataProvider
    {
        private readonly HttpClient _httpClient;
        private readonly DataSourceConfig _dataSourceConfig;
        private readonly ILogger<SaveLifeDataProvider> _logger;

        public SaveLifeDataProvider(
            HttpClient httpClient,
            IOptions<DataSourceConfig> options,
            ILogger<SaveLifeDataProvider> logger)
        {
            _httpClient = httpClient;
            _dataSourceConfig = options.Value;
            _logger = logger;
        }

        public async Task<SLOriginResponse> LoadDataAsync(SLOriginDataRequest request, CancellationToken cancellationToken)
        {
            var requestUrl = $"{_dataSourceConfig.BaseUrl}/{_dataSourceConfig.EndpointTemplate}"
                .Replace("{DATE_FROM}", request.DateFromString)
                .Replace("{DATE_TO}", request.DateToString)
                .Replace("{PAGE}", request.Page.ToString())
                .Replace("{PER_PAGE}", _dataSourceConfig.BatchSize.ToString());


            var httpResponse = await _httpClient.GetAsync(requestUrl, cancellationToken);
            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new ArgumentException($"Loading failed. Status code {httpResponse.StatusCode}. Content: {await httpResponse.Content.ReadAsStringAsync()}");
            }

            var content = await httpResponse.Content.ReadAsStringAsync();
            var response = content.Deserialize<SLOriginResponse>();

            return response!;
        }
    }
}
