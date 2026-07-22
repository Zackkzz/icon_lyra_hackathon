using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FridgeMealPlanner.Models;

// A grocery spend event — recorded when a scanned/entered receipt is confirmed.
public class Purchase
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    public int ItemCount { get; set; }
}
