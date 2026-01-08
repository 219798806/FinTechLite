using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinTechLiteAPI.Data;
using FinTechLiteAPI.Models.Domain;
using Org.BouncyCastle.Crypto.Generators;

namespace FinTechLiteAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user
        /// Steps:
        /// 1. Validate the input (check for empty fields)
        /// 2. Check if username or email already exists
        /// 3. Hash the password (never store plain passwords!)
        /// 4. Create the User record
        /// 5. Create an Account for the user with initial balance
        /// 6. Save to database
        /// </summary>
        public async Task<(bool Success, string? Error, Guid? UserId, Guid? AccountId)>
            RegisterAsync(string username, string email, string password, decimal? initialBalance)
        {
            try
            {
                // Step 1: Validate input
                if (string.IsNullOrWhiteSpace(username))
                    return (false, "Username is required", null, null);

                if (string.IsNullOrWhiteSpace(email))
                    return (false, "Email is required", null, null);

                if (string.IsNullOrWhiteSpace(password))
                    return (false, "Password is required", null, null);

                // Step 2: Check if user already exists
                var userExists = await _context.Users.AnyAsync(u => u.Username == username);
                if (userExists)
                    return (false, "Username already taken", null, null);

                var emailExists = await _context.Users.AnyAsync(u => u.Email == email);
                if (emailExists)
                    return (false, "Email already registered", null, null);

                // Step 3: Hash the password using BCrypt (secure hashing)
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                // Step 4: Create the user
                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    Username = username,
                    Email = email,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(user);

                // Step 5: Create an account for the user
                var account = new Account
                {
                    AccountId = Guid.NewGuid(),
                    UserId = user.UserId,
                    Balance = initialBalance,
                    Version = 0, // For tracking concurrent updates
                    CreatedAt = DateTime.UtcNow
                };
                _context.Accounts.Add(account);

                // Step 6: Save everything to database
                await _context.SaveChangesAsync();

                _logger.LogInformation("User registered: {Username} with balance {Balance:C}", username, initialBalance);

                return (true, null, user.UserId, account.AccountId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed: {Message}", ex.Message);
                return (false, "Registration failed. Please try again.", null, null);
            }
        }

        /// <summary>
        /// Login a user
        /// Steps:
        /// 1. Find the user by username
        /// 2. Verify the password (compare hash)
        /// 3. Generate a JWT token if successful
        /// 4. Return the token
        /// </summary>
        public async Task<(bool Success, string? Token, string? Error, Guid? UserId, Guid? AccountId, decimal? Balance)>
            LoginAsync(string username, string password)
        {
            try
            {
                // Step 1: Find the user
                var user = await _context.Users
                    .Include(u => u.Account) // Also load their account
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                    return (false, null, "Invalid username or password", null, null, null);

                // Step 2: Verify password
                var isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                if (!isPasswordValid)
                    return (false, null, "Invalid username or password", null, null, null);

                // Step 3: Generate JWT token
                var token = GenerateJwtToken(user);

                _logger.LogInformation("User logged in: {Username}", username);

                return (true, token, null, null, null, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed: {Message}", ex.Message);
                return (false, null, "Login failed. Please try again.", null, null, null);
            }
        }

        /// <summary>
        /// Generate a JWT token for authenticated users
        /// JWT = JSON Web Token (a secure way to authenticate API requests)
        /// </summary>
        private string GenerateJwtToken(User user)
        {
            // Get JWT settings from configuration
            var key = _configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
            var issuer = _configuration["Jwt:Issuer"] ?? "FinTechLite";
            var audience = _configuration["Jwt:Audience"] ?? "FinTechLiteUsers";

            // Create the encryption key
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Add user information to the token
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            // Create the token
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1), // Token valid for 1 hour
                signingCredentials: credentials
            );

            // Convert token to string
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}