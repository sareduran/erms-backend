using ERMS.Domain.Enums;
namespace ERMS.Domain.Entities;
public sealed class RequestHistory
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public EmployeeRequest? Request { get; set; }
    public int ChangedById { get; set; }
    public User? ChangedBy { get; set; }
    public RequestStatus? OldStatus { get; set; }
    public RequestStatus NewStatus { get; set; }
    public required string Action { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
