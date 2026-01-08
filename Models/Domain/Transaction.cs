using FinTechLiteAPI.Models.Enums;

namespace FinTechLiteAPI.Models.Domain
{
    public class Transaction
    {
        public Guid TransactionId { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
        public Guid? FromAccountId { get; set; }
        public Guid? ToAccountId { get; set; }
        public decimal Amount { get; set; }
        public TransactionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? FailureReason { get; set; }
        // Navigation properties
        public Account? FromAccount { get; set; }
        public Account? ToAccount { get; set; }
        public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
    }
}
