namespace SaveLife.Stats.Domain.Models
{
    public class DonatorEntity
    {
        public string Id { get; set; }
        public string Identity { get; set; }
        public double TotalDonation { get; set; }
        public int TransactionsCount { get; set; }
        public double LastTransactionStamp { get; set; }
    }
}
