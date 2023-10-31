using System.Text.Json.Serialization;

namespace SaveLife.Stats.Worker.Models
{
    public class SLOriginResponse
    {
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("rows")]
        public IList<SLOriginTransaction> Transactions { get; set; }
    }

    public class SLOriginTransaction
    {
        public long Id { get; set; }
        public string Amount { get; set; }
        public string Comment { get; set; }
        public string Currency { get; set; }
        public string Source { get; set; }
        public DateTime Date { get; set; }
    }
}
