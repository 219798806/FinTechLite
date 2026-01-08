using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FinTechLiteAPI.Models.Domain;

namespace FinTechLiteAPI.Data.Configurations
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.ToTable("Transactions");

            builder.HasKey(e => e.TransactionId);

            builder.Property(e => e.TransactionId)
                .ValueGeneratedOnAdd();

            builder.Property(e => e.IdempotencyKey)
                .HasMaxLength(100)
                .IsRequired();

            builder.HasIndex(e => e.IdempotencyKey)
                .IsUnique()
                .HasDatabaseName("IX_Transactions_IdempotencyKey");

            builder.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(e => new { e.FromAccountId, e.ToAccountId })
                .HasDatabaseName("IX_Transactions_FromTo");

            builder.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_Transactions_CreatedAt");

            // Relationships
            builder.HasOne(e => e.FromAccount)
                .WithMany(a => a.SentTransactions)
                .HasForeignKey(e => e.FromAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.ToAccount)
                .WithMany(a => a.ReceivedTransactions)
                .HasForeignKey(e => e.ToAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            // SQL Server CHECK constraint
            builder.ToTable(t => t.HasCheckConstraint("CK_Transactions_Amount", "[Amount] > 0"));
        }
    }
}