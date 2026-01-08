using Microsoft.EntityFrameworkCore;
using FinTechLiteAPI.Data;
using FinTechLiteAPI.Models.Domain;
using FinTechLiteAPI.Models.Enums;
using System.Data;

namespace FinTechLiteAPI.Services;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(AppDbContext context, ILogger<TransactionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Transfer money between accounts with Resiliency and ACID compliance.
    /// </summary>
    public async Task<(bool Success, string? Error, Guid? TransactionId)>
        TransferMoneyAsync(Guid fromAccountId, Guid toAccountId, decimal amount)
    {
        // STEP 1: Basic validation (Outside the transaction to save resources)
        if (amount <= 0)
            return (false, "Amount must be greater than zero", null);

        if (fromAccountId == toAccountId)
            return (false, "Cannot transfer to the same account", null);

        // --- FIX: Create the Execution Strategy for Retries ---
        var strategy = _context.Database.CreateExecutionStrategy();

        // --- FIX: Execute the transaction inside the strategy ---
        // Tell the strategy we expect a Tuple back
        return await strategy.ExecuteAsync<(bool, string?, Guid?)>(async () =>
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                // STEP 3: Lock both accounts to prevent concurrent transfers
                // We lock in a specific order to prevent deadlocks
                var accountIds = new[] { fromAccountId, toAccountId }.OrderBy(id => id).ToArray();

                var accounts = await _context.Accounts
                    .FromSqlRaw(@"
                        SELECT * FROM Accounts WITH (UPDLOCK, ROWLOCK) 
                        WHERE AccountId IN ({0}, {1})",
                        accountIds[0], accountIds[1])
                    .ToListAsync();

                var senderAccount = accounts.FirstOrDefault(a => a.AccountId == fromAccountId);
                var receiverAccount = accounts.FirstOrDefault(a => a.AccountId == toAccountId);

                // STEP 4: Check if accounts exist
                if (senderAccount == null)
                {
                    // Rollback manually before returning logic failure
                    await dbTransaction.RollbackAsync();
                    return (false, "Sender account not found", null);
                }

                if (receiverAccount == null)
                {
                    await dbTransaction.RollbackAsync();
                    return (false, "Receiver account not found", null);
                }

                // STEP 5: Check if sender has enough money
                if (senderAccount.Balance < amount)
                {
                    await dbTransaction.RollbackAsync();
                    var message = $"Insufficient funds. You have {senderAccount.Balance:C}, need {amount:C}";
                    return (false, message, null);
                }

                // STEP 6: Create transaction record
                var transaction = new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    IdempotencyKey = Guid.NewGuid().ToString(),
                    FromAccountId = fromAccountId,
                    ToAccountId = toAccountId,
                    Amount = amount,
                    Status = TransactionStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                // STEP 7: Update balances & Version (Optimistic Locking)
                senderAccount.Balance -= amount;
                senderAccount.Version++;

                receiverAccount.Balance += amount;
                receiverAccount.Version++;

                // STEP 8: Create ledger entries
                var debitEntry = new LedgerEntry
                {
                    EntryId = Guid.NewGuid(),
                    TransactionId = transaction.TransactionId,
                    AccountId = senderAccount.AccountId,
                    DebitAmount = amount,
                    CreditAmount = null,
                    BalanceAfter = senderAccount.Balance,
                    CreatedAt = DateTime.UtcNow
                };

                var creditEntry = new LedgerEntry
                {
                    EntryId = Guid.NewGuid(),
                    TransactionId = transaction.TransactionId,
                    AccountId = receiverAccount.AccountId,
                    DebitAmount = null,
                    CreditAmount = amount,
                    BalanceAfter = receiverAccount.Balance,
                    CreatedAt = DateTime.UtcNow
                };

                _context.LedgerEntries.AddRange(debitEntry, creditEntry);

                // STEP 9: Mark transaction as completed
                transaction.Status = TransactionStatus.Completed;
                transaction.CompletedAt = DateTime.UtcNow;

                // STEP 10: Save all changes
                await _context.SaveChangesAsync();

                // STEP 11: Commit the transaction
                await dbTransaction.CommitAsync();

                _logger.LogInformation(
                    "Transfer completed: {Amount:C} from Account {From} to Account {To}",
                    amount, fromAccountId, toAccountId);

                return (true, null, transaction.TransactionId);
            }
            catch (DbUpdateConcurrencyException)
            {
                await dbTransaction.RollbackAsync();
                return (false, "Transaction failed due to concurrent update. Please try again.", null);
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Transfer failed: {Message}", ex.Message);
                // If we just return false, the strategy thinks it succeeded and won't retry transient errors.
                throw;
            }
        });
    }
}