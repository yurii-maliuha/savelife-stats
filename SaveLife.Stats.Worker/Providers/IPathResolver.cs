namespace SaveLife.Stats.Worker.Providers
{
    public interface IPathResolver
    {
        string ResolveTransactionsPath();
        string ResolveHistoryPath();
    }
}
