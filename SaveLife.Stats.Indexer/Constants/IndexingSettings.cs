namespace SaveLife.Stats.Indexer.Constants
{
    public static class IndexingSettings
    {
        public const int PublisherBatchSize = 500;
        public const int PublisherMaxInterations = 2;
        public const int PublisherCollectionCapacity = 5000;

        public const int IndexerBatchSizue = 100;
        public const int IndexerParallelismDegree = 16;
    }
}
