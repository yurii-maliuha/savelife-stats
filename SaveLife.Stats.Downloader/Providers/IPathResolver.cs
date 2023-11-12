namespace SaveLife.Stats.Downloader.Providers
{
    public interface IPathResolver
    {
        string ResolveTransactionsPath();
        string ResolveHistoryPath();
    }
}
