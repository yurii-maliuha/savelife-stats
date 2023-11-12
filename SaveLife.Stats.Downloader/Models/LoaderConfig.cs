namespace SaveLife.Stats.Downloader.Models
{
    public class LoaderConfig
    {
        public const string DisplayName = "Loader";
        public int MaxIterationsCount { get; set; }
        public string LoadFrom { get; set; }
        public DateTime LoadFromDate => DateTime.Parse(LoadFrom);
        public string LoadTo { get; set; }
        public DateTime LoadToDate => DateTime.Parse(LoadTo);
        public int ThrottleSeconds { get; set; }
        public int MaxSeccondsPerOperation { get; set; }
    }
}
