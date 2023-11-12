namespace SaveLife.Stats.Downloader.Models
{
    public class DataSourceConfig
    {
        public const string DisplayName = "DataSource";
        public string BaseUrl { get; set; }
        public string EndpointTemplate { get; set; }
        public int BatchSize { get; set; }
    }
}
