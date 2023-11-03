using SaveLife.Stats.Worker.Providers;

namespace SaveLife.Stats.Worker.Tests
{
    public class TestPathResolver : IPathResolver
    {
        public string ResolveHistoryPath()
            => @$"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\";

        public string ResolveTransactionsPath()
            => @$"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\Data";
    }
}
