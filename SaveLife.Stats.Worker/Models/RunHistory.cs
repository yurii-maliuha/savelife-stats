namespace SaveLife.Stats.Worker.Models
{
    public class RunHistory
    {
        public long? LastTransactionId { get; set; }

        public DateTime DateFrom { get; set; }

        public DateTime DateTo { get; set; }

        public int Page { get; set; }

    }
}
