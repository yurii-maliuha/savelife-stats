namespace SaveLife.Stats.Indexer.Models
{
    public class ElasticsearchConfig
    {
        public const string DisplayName = "Elasticsearch";
        public string Uri { get; set; }
        public bool RunFromScratch { get; set; }
    }
}
