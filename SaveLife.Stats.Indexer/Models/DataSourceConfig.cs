namespace SaveLife.Stats.Indexer.Models
{
    public class DataSourceConfig
    {
        public const string DisplayName = "DataSource";
        public string Path { get; set; }
        public int BatchSize { get; set; }
        public int PublisherMaxInterations { get; set; }
    }
}
