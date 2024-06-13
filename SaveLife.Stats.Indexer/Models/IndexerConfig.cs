namespace SaveLife.Stats.Indexer.Models
{
    public class IndexerConfig
    {
        public const string DisplayName = "IndexerConfig";
        public bool Enable { get; set; }
        public string SourcePath { get; set; } = string.Empty;
        public int BatchSize { get; set; }
        public int PublisherMaxInterations { get; set; }
        public int StartFileLine { get; set; }
    }
}
