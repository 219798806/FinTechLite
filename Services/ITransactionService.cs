using FinTechLiteAPI.Models.DTOs;

namespace FinTechLiteAPI.Services
{
    public interface ITransactionService
    {
        // Transfer money from one account to another
        Task<(bool Success, string? Error, Guid? TransactionId)>
            TransferMoneyAsync(Guid fromAccountId, Guid toAccountId, decimal amount);
    }
}