namespace FinTechLiteAPI.Models.Domain
{
    public class LedgerEntry
    {
        public Guid EntryId { get; set; }
        public Guid TransactionId { get; set; }
        public Guid AccountId { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public decimal? BalanceAfter { get; set; }
        public DateTime CreatedAt { get; set; }
        // Navigation properties
        public Transaction Transaction { get; set; } = null!;
        public Account Account { get; set; } = null!;
    }
}
