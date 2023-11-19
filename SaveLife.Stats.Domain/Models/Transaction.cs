namespace SaveLife.Stats.Domain.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        public string Amount { get; set; }

        public string Comment { get; set; }

        public string Currency { get; set; }

        public string Source { get; set; }

        public DateTime Date { get; set; }
        public string? CardNumber { get; set; }
        public string? FullName { get; set; }
    }
}
