using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FinTechLiteAPI.Models.Domain;

namespace FinTechLiteAPI.Data.Configurations
{
    public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
    {
        public void Configure(EntityTypeBuilder<LedgerEntry> builder)
        {
            builder.ToTable("LedgerEntries");

            builder.HasKey(e => e.EntryId);

            builder.Property(e => e.EntryId)
                .ValueGeneratedOnAdd();

            builder.Property(e => e.DebitAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(e => e.CreditAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(e => e.BalanceAfter)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(e => new { e.AccountId, e.CreatedAt })
                .HasDatabaseName("IX_LedgerEntries_AccountId_CreatedAt");

            // Relationships
            builder.HasOne(e => e.Transaction)
                .WithMany(t => t.LedgerEntries)
                .HasForeignKey(e => e.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Account)
                .WithMany(a => a.LedgerEntries)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // SQL Server CHECK constraint - Either Debit or Credit, not both
            builder.ToTable(t => t.HasCheckConstraint("CK_LedgerEntries_DebitOrCredit",
                "([DebitAmount] IS NOT NULL AND [CreditAmount] IS NULL) OR " +
                "([DebitAmount] IS NULL AND [CreditAmount] IS NOT NULL)"));
        }
    }
}