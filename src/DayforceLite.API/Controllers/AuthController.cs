using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using DayforceLite.API.DTOs;

namespace DayforceLite.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        // Simplified authentication - in production, validate against database
        if (!ValidateCredentials(request.Username, request.Password))
        {
            return Unauthorized("Invalid credentials");
        }

        var token = GenerateJwtToken(request.Username);
        var expiresAt = DateTime.UtcNow.AddHours(1);

        _logger.LogInformation("User {Username} logged in successfully", request.Username);

        return Ok(new LoginResponse(token, expiresAt));
    }

    [HttpPost("refresh")]
    [Authorize]
    public ActionResult<LoginResponse> Refresh([FromBody] RefreshTokenRequest request)
    {
        // Validate the existing token and issue a new one
        var username = User.Identity?.Name ?? "user";
        var token = GenerateJwtToken(username);
        var expiresAt = DateTime.UtcNow.AddHours(1);

        return Ok(new LoginResponse(token, expiresAt));
    }

    private bool ValidateCredentials(string username, string password)
    {
        // Simplified validation - replace with proper user store validation
        return !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password);
    }

    private string GenerateJwtToken(string username)
    {
        var key = _configuration["Jwt:Key"] ?? "DayforceLiteSecretKey12345678901234567890";
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "DayforceLite",
            audience: _configuration["Jwt:Audience"] ?? "DayforceLite",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
