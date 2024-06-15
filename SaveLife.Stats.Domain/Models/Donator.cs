namespace SaveLife.Stats.Domain.Models
{
    public class Donator
    {
        public string Identity { get; set; }
        public double TotalDonation { get; set; }
        public int TransactionsCount { get; set; }
        public double LastTransactionStamp { get; set; }
    }
}
