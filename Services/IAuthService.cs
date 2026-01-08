namespace FinTechLiteAPI.Services
{
    public interface IAuthService
    {
        // Register a new user and create their account
        Task<(bool Success, string? Error, Guid? UserId, Guid? AccountId)>
            RegisterAsync(string username, string email, string password, decimal? initialBalance);

        // Login and get a JWT token
        Task<(bool Success, string? Token, string? Error, Guid? UserId, Guid? AccountId, decimal? Balance)>
            LoginAsync(string username, string password);
    }
    //public interface IAuthService
    //{
    //    Task<string> GenerateJwtToken(Guid userId, string username, string email);
    //    Task<(bool Success, string? Token, string? Error, Guid? UserId, Guid? AccountId, decimal? Balance)>
    //        LoginAsync(string username, string password);
    //    Task<(bool Success, string? Error, Guid? UserId, Guid? AccountId)>
    //        RegisterAsync(string username, string email, string password, decimal? initialBalance);
    //}
}