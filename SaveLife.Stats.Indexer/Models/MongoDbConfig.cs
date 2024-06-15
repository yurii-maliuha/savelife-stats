namespace SaveLife.Stats.Indexer.Models
{
    public class MongoDbConfig
    {
        public string DatabaseName { get; set; }

        public string HostName { get; set; }

        public string ReplicaSet { get; set; }

        public int Port { get; set; }
    }
}
