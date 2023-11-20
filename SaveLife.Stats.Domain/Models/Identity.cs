namespace SaveLife.Stats.Domain.Models
{
    public class Identity
    {
        public string? FullName { get; set; }
        public string? CardNumber { get; set; }
        public string? LegalName { get; set; }

        public TransactionRecord Transaction { get; set; }
    }

    public class TransactionRecord
    {
        public int Id { get; set; }
        public string Ammount { get; set; }
        public string Currency { get; set; }
        public DateTime Date { get; set; }
    }
}
