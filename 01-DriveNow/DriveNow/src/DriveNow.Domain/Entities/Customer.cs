using DriveNow.Domain.Enums;

namespace DriveNow.Domain.Entities;

public class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public DateTime LicenseIssueDate { get; set; }
    public LoyaltyTier LoyaltyTier { get; set; }
    public bool IsBlacklisted { get; set; }
    public int CompletedRentals { get; set; }
}
