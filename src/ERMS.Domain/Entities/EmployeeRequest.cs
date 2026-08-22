using ERMS.Domain.Enums;
namespace ERMS.Domain.Entities;
public sealed class EmployeeRequest
{
    public int Id { get; set; }
    public int RequestTypeId { get; set; }
    public RequestType? RequestType { get; set; }
    public int RequesterId { get; set; }
    public User? Requester { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public RequestStatus Status { get; set; }
    public RequestPriority Priority { get; set; } = RequestPriority.Normal;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Approval> Approvals { get; set; } = [];
    public ICollection<RequestComment> Comments { get; set; } = [];
    public ICollection<RequestAttachment> Attachments { get; set; } = [];
    public ICollection<RequestHistory> History { get; set; } = [];
}
