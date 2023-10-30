using System.Text.Json.Serialization;

namespace SaveLife.Stats.Domain.Models
{
    public class SLTransaction
    {
        public int Id { get; set; }

        [JsonPropertyName("amt")]
        public string Amount { get; set; }

        [JsonPropertyName("cmt")]
        public string Comment { get; set; }

        [JsonPropertyName("cur")]
        public string Currency { get; set; }

        [JsonPropertyName("src")]
        public string Source { get; set; }

        [JsonPropertyName("dt")]
        public DateTime Date { get; set; }
    }
}
