using System.ComponentModel.DataAnnotations;

namespace FridgeMealPlanner.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    // PBKDF2 hash stored as "iterations.base64salt.base64hash"
    [Required]
    [MaxLength(400)]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
