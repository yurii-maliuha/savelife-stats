using SaveLife.Stats.Downloader.Providers;

namespace SaveLife.Stats.Downloader.Tests.Stubs
{
    public class PathResolverStub : IPathResolver
    {
        public string ResolveHistoryPath()
            => @$"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\Data";

        public string ResolveTransactionsPath()
            => @$"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\Data";
    }
}
