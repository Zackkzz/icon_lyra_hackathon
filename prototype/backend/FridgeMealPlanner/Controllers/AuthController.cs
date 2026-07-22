using System.Text.RegularExpressions;
using FridgeMealPlanner.Data;
using FridgeMealPlanner.DTOs;
using FridgeMealPlanner.Extensions;
using FridgeMealPlanner.Models;
using FridgeMealPlanner.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FridgeMealPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _jwt;

    public AuthController(AppDbContext db, JwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var email = (request.Email ?? "").Trim().ToLowerInvariant();

        if (!IsValidEmail(email))
            return BadRequest(new { error = "A valid email is required." });
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return BadRequest(new { error = "Password must be at least 6 characters." });

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { error = "An account with that email already exists." });

        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? email.Split('@')[0]
            : request.DisplayName!.Trim();

        var user = new User
        {
            Email = email,
            DisplayName = displayName,
            PasswordHash = PasswordHasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _jwt.CreateToken(user);
        return Ok(new AuthResponse(token, user.Id, user.Email, user.DisplayName));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var email = (request.Email ?? "").Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !PasswordHasher.Verify(request.Password ?? "", user.PasswordHash))
            return Unauthorized(new { error = "Invalid email or password." });

        var token = _jwt.CreateToken(user);
        return Ok(new AuthResponse(token, user.Id, user.Email, user.DisplayName));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthResponse>> Me()
    {
        var user = await _db.Users.FindAsync(User.GetUserId());
        if (user == null) return Unauthorized();
        return Ok(new AuthResponse("", user.Id, user.Email, user.DisplayName));
    }

    private static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) &&
        Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
}
