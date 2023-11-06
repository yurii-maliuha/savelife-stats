using SaveLife.Stats.Worker.Providers;

namespace SaveLife.Stats.Worker.Tests.Stubs
{
    public class PathResolverStub : IPathResolver
    {
        public string ResolveHistoryPath()
            => @$"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\Data";

        public string ResolveTransactionsPath()
            => @$"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\Data";
    }
}
