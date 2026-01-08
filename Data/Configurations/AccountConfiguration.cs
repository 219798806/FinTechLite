using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FinTechLiteAPI.Models.Domain;

namespace FinTechLiteAPI.Data.Configurations
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("Accounts");

            builder.HasKey(e => e.AccountId);

            builder.Property(e => e.AccountId)
                .ValueGeneratedOnAdd();

            builder.Property(e => e.Balance)
                .HasColumnType("decimal(18,2)")
                .IsRequired()
                .HasDefaultValue(0.00m);

            builder.Property(e => e.Version)
                .IsConcurrencyToken() // Enable optimistic concurrency
                .HasDefaultValue(0);

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_Accounts_UserId");

            // One User has One Account
            builder.HasOne(e => e.User)
                .WithOne(u => u.Account)
                .HasForeignKey<Account>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // SQL Server CHECK constraint syntax
            builder.ToTable(t => t.HasCheckConstraint("CK_Accounts_Balance", "[Balance] >= 0"));
        }
    }
}