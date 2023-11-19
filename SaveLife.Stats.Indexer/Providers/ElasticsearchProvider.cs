using Nest;
using SaveLife.Stats.Domain.Models;
using SaveLife.Stats.Indexer.Constants;
using System.Runtime.ExceptionServices;

namespace SaveLife.Stats.Indexer.Providers
{
    public class ElasticsearchProvider
    {
        private readonly IElasticClient _client;

        public ElasticsearchProvider(
            IElasticClient client)
        {
            _client = client;
        }

        public void IndexInParrarel(IEnumerable<Transaction> transactions)
        {
            var handle = new ManualResetEvent(false);
            var seenPages = 0;
            var observableBulk = _client.BulkAll(transactions, f => f
                .MaxDegreeOfParallelism(IndexingSettings.IndexerParallelismDegree)
                .BackOffTime(TimeSpan.FromSeconds(10))
                .BackOffRetries(2)
                .Size(IndexingSettings.IndexerBatchSizue)
                .RefreshOnCompleted()
            );

            ExceptionDispatchInfo exception = null;
            var bulkObserver = new BulkAllObserver(
                onError: e =>
                {
                    exception = ExceptionDispatchInfo.Capture(e);
                    handle.Set();
                },
                onCompleted: () => handle.Set(),
                onNext: b =>
                {
                    Interlocked.Increment(ref seenPages);
                    Console.WriteLine($"[{DateTime.Now}]: Indexed {seenPages} pages");
                }
            );

            observableBulk.Subscribe(bulkObserver);
            handle.WaitOne();

            if (exception != null)
                exception.Throw();
        }
    }
}
