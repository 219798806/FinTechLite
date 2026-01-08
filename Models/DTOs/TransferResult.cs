namespace FinTechLiteAPI.Models.DTOs
{
    public record TransferResult(bool Success, string? ErrorMessage, Guid? TransactionId);
}
