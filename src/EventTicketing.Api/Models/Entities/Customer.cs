namespace EventTicketing.Models.Entities;

public class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty; // unique
    public string? Phone { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
