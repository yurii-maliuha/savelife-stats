namespace SaveLife.Stats.Worker.Models
{
    public class DataSourceConfig
    {
        public const string DisplayName = "DataSource";
        public string BaseUrl { get; set; }
        public string EndpointTemplate { get; set; }
        public int PerPage { get; set; }
    }
}
