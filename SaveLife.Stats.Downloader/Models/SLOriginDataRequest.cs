namespace SaveLife.Stats.Downloader.Models
{
    public class SLOriginDataRequest
    {
        public DateTime DateFrom { get; set; }
        public string DateFromString
             => DateFrom.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
        public DateTime DateTo { get; set; } = DateTime.Now;
        public string DateToString
            => DateTo.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");

        public int Page { get; set; } = 1;

    }
}
