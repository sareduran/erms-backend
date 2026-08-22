using ERMS.Domain.Enums;
namespace ERMS.Domain.Entities;
public sealed class User
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; }
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int? ManagerId { get; set; }
    public User? Manager { get; set; }
    public ICollection<User> DirectReports { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string FullName => $"{FirstName} {LastName}";
}
