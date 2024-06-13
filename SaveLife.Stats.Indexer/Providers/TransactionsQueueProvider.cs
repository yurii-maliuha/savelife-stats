using Microsoft.Extensions.Options;
using SaveLife.Stats.Domain.Models;
using SaveLife.Stats.Indexer.Models;
using System.Threading.Channels;

namespace SaveLife.Stats.Indexer.Providers
{
    public class TransactionsQueueProvider
    {
        private readonly Channel<SLTransaction> _channel;
        public ChannelReader<SLTransaction> Reader => _channel.Reader;
        public ChannelWriter<SLTransaction> Writer => _channel.Writer;

        public TransactionsQueueProvider(
            IOptions<IndexerConfig> sourceConfigOptions)
        {
            _channel = Channel.CreateBounded<SLTransaction>(sourceConfigOptions.Value.BatchSize);
        }

    }
}
