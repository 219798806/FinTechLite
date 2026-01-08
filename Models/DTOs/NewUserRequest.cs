namespace FinTechLiteAPI.Models.DTOs
{
    public record NewUserRequest(string Username, string Email, string Password, decimal? InitialBalance = null);
}
