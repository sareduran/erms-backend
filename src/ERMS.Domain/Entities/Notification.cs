namespace ERMS.Domain.Entities;
public sealed class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int? RequestId { get; set; }
    public EmployeeRequest? Request { get; set; }
    public required string Type { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
