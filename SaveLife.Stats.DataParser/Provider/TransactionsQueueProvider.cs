using SaveLife.Stats.Domain.Models;
using System.Threading.Channels;

namespace SaveLife.Stats.DataParser.Provider
{
    public class TransactionsQueueProvider
    {
        private const int BatchSize = 5;
        private readonly Channel<SLTransaction> _channel;
        public ChannelReader<SLTransaction> Reader => _channel.Reader;
        public ChannelWriter<SLTransaction> Writer => _channel.Writer;

        public TransactionsQueueProvider()
        {
            _channel = Channel.CreateBounded<SLTransaction>(BatchSize);
        }

    }
}
