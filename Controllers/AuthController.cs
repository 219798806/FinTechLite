using Microsoft.AspNetCore.Mvc;
using FinTechLiteAPI.Models.DTOs;
using FinTechLiteAPI.Services;

namespace FinTechLiteAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] NewUserRequest request)
    {
        var result = await _authService.RegisterAsync(
            request.Username,
            request.Email,
            request.Password,
            request.InitialBalance);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            userId = result.UserId,
            username = request.Username,
            accountId = result.AccountId,
            message = "Registration successful"
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] ExistingUserRequest request)
    {
        var result = await _authService.LoginAsync(request.Username, request.Password);

        if (!result.Success)
            return Unauthorized(new { error = result.Error });

        return Ok(new
        {
            token = result.Token,
            userId = result.UserId,
            username = request.Username,
            accountId = result.AccountId,
            balance = result.Balance
        });
    }
}