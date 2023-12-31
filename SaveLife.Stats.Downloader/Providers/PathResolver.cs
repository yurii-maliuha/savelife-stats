﻿namespace SaveLife.Stats.Downloader.Providers
{
    public class PathResolver : IPathResolver
    {
        public string ResolveHistoryPath()
            => @$"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\";

        public string ResolveTransactionsPath()
            => @$"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\..\SaveLife.Stats.Data\Raw";
    }
}
