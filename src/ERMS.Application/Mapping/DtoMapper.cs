using ERMS.Application.DTOs;
using ERMS.Domain.Entities;
namespace ERMS.Application.Mapping;
public static class DtoMapper
{
    public static UserSummary ToSummary(this User user) => new(user.Id, user.FullName, user.Email, user.Role, user.Department?.Name ?? "-", user.ManagerId, user.IsActive, user.MustChangePassword);
    public static RequestListItem ToListItem(this EmployeeRequest request) => new(request.Id, request.Title, request.RequestType?.Name ?? "-", request.Status, request.Priority, request.Requester?.FullName ?? "-", request.CreatedAt);
    public static RequestDetailDto ToDetail(this EmployeeRequest request) => new(
        request.Id, request.RequestTypeId, request.RequestType?.Name ?? "-", request.RequesterId, request.Requester?.FullName ?? "-", request.Title, request.Description, request.Status, request.Priority, request.StartDate, request.EndDate, request.Amount, request.CreatedAt, request.UpdatedAt,
        request.Approvals.OrderBy(x => x.DecidedAt).Select(x => new ApprovalDto(x.Id, x.Decision.ToString(), x.Approver?.FullName ?? "-", x.Comment, x.DecidedAt)).ToList(),
        request.Comments.OrderBy(x => x.CreatedAt).Select(x => new CommentDto(x.Id, x.Author?.FullName ?? "-", x.Content, x.CreatedAt)).ToList(),
        request.Attachments.OrderBy(x => x.CreatedAt).Select(x => new AttachmentDto(x.Id, x.FileName, x.ContentType, x.FileSize, x.CreatedAt)).ToList(),
        request.History.OrderBy(x => x.ChangedAt).Select(x => new HistoryDto(x.Id, x.RequestId, x.OldStatus?.ToString(), x.NewStatus.ToString(), x.Action, x.ChangedBy?.FullName ?? "-", x.ChangedAt)).ToList());
}
