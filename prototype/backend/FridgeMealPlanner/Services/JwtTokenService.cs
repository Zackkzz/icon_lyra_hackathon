using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FridgeMealPlanner.Models;
using Microsoft.IdentityModel.Tokens;

namespace FridgeMealPlanner.Services;

public class JwtTokenService
{
    public const string Issuer = "fridge-meal-planner";
    public const string Audience = "fridge-meal-planner-app";

    private readonly SymmetricSecurityKey _key;

    public JwtTokenService(IConfiguration config)
    {
        // Prefer env var, then appsettings, then a dev fallback so the app runs out of the box.
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? config["Jwt:Secret"]
            ?? "dev-only-insecure-secret-change-me-please-32bytes!";
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }

    public SymmetricSecurityKey SigningKey => _key;

    public string CreateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.DisplayName),
        };

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
