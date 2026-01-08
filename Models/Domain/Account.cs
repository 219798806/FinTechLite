namespace FinTechLiteAPI.Models.Domain
{
    public class Account
    {
        public Guid AccountId { get; set; }
        public Guid UserId { get; set; }
        public decimal? Balance { get; set; }
        public int Version { get; set; } // For optimistic concurrency
        public DateTime CreatedAt { get; set; }
        // Navigation properties
        public User User { get; set; } = null!;
        public ICollection<Transaction> SentTransactions { get; set; } = new List<Transaction>();
        public ICollection<Transaction> ReceivedTransactions { get; set; } = new List<Transaction>();
        public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
    }
}
