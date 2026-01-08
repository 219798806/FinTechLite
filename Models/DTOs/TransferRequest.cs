namespace FinTechLiteAPI.Models.DTOs
{
    public record TransferRequest(Guid ToAccountId, decimal Amount, string? IdempotencyKey = null);
}
