using ERMS.Domain.Enums;
namespace ERMS.Domain.Entities;
public sealed class Approval
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public EmployeeRequest? Request { get; set; }
    public int ApproverId { get; set; }
    public User? Approver { get; set; }
    public ApprovalDecision Decision { get; set; }
    public string? Comment { get; set; }
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
}
