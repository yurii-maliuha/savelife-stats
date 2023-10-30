namespace SaveLife.Stats.Domain.Models
{
    public class Identity
    {
        public string? FullName { get; set; }
        public string? CardNumber { get; set; }

        public Transaction Transaction { get; set; }
    }

    public class Transaction
    {
        public int Id { get; set; }
        public string Ammount { get; set; }
        public string Currency { get; set; }
        public DateTime Date { get; set; }
    }
}
