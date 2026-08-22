using ERMS.Application.Interfaces;
using ERMS.Domain.Entities;
using ERMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace ERMS.Infrastructure.Persistence;
public sealed class ErmsRepository(AppDbContext db) : IErmsRepository
{
    public Task<User?> FindUserByEmailAsync(string email, CancellationToken ct) => db.Users.Include(x => x.Department).FirstOrDefaultAsync(x => x.Email == email, ct);
    public Task<User?> FindUserAsync(int id, CancellationToken ct) => db.Users.Include(x => x.Department).FirstOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken ct) => await db.Users.Include(x => x.Department).OrderBy(x => x.FirstName).ThenBy(x => x.LastName).ToListAsync(ct);
    public async Task AddUserAsync(User user, CancellationToken ct) => await db.Users.AddAsync(user, ct);
    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(bool activeOnly, CancellationToken ct) => await db.Departments.Where(x => !activeOnly || x.IsActive).OrderBy(x => x.Name).ToListAsync(ct);
    public Task<Department?> FindDepartmentAsync(int id, CancellationToken ct) => db.Departments.FirstOrDefaultAsync(x => x.Id == id, ct);
    public async Task AddDepartmentAsync(Department department, CancellationToken ct) => await db.Departments.AddAsync(department, ct);
    public async Task<IReadOnlyList<RequestType>> GetRequestTypesAsync(bool activeOnly, CancellationToken ct) => await db.RequestTypes.Where(x => !activeOnly || x.IsActive).OrderBy(x => x.Name).ToListAsync(ct);
    public Task<RequestType?> FindRequestTypeAsync(int id, CancellationToken ct) => db.RequestTypes.FirstOrDefaultAsync(x => x.Id == id, ct);
    public async Task AddRequestTypeAsync(RequestType requestType, CancellationToken ct) => await db.RequestTypes.AddAsync(requestType, ct);
    public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct) => await db.RefreshTokens.AddAsync(token, ct);
    public Task<RefreshToken?> FindRefreshTokenAsync(string tokenHash, CancellationToken ct) => db.RefreshTokens.Include(x => x.User).ThenInclude(x => x!.Department).FirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);
    public async Task RevokeRefreshTokensAsync(int userId, CancellationToken ct) => await db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAt == null).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevokedAt, DateTime.UtcNow), ct);
    public Task<EmployeeRequest?> FindRequestAsync(int id, bool includeDetails, CancellationToken ct)
    {
        IQueryable<EmployeeRequest> query = db.Requests.Include(x => x.RequestType).Include(x => x.Requester);
        if (includeDetails) query = query.Include(x => x.Approvals).ThenInclude(x => x.Approver).Include(x => x.Comments).ThenInclude(x => x.Author).Include(x => x.Attachments).Include(x => x.History).ThenInclude(x => x.ChangedBy);
        return query.AsSplitQuery().FirstOrDefaultAsync(x => x.Id == id, ct);
    }
    public async Task AddRequestAsync(EmployeeRequest request, CancellationToken ct) => await db.Requests.AddAsync(request, ct);
    public async Task<(IReadOnlyList<EmployeeRequest> items, int total)> SearchRequestsAsync(int actorId, UserRole role, RequestStatus? status, int? typeId, DateTime? from, DateTime? to, string? search, int page, int pageSize, bool pendingForManager, CancellationToken ct)
    {
        IQueryable<EmployeeRequest> query = db.Requests.Include(x => x.RequestType).Include(x => x.Requester);
        query = pendingForManager ? query.Where(x => x.Requester!.ManagerId == actorId && x.Status == RequestStatus.Pending) : role == UserRole.Admin ? query : query.Where(x => x.RequesterId == actorId);
        if (status.HasValue) query = query.Where(x => x.Status == status); if (typeId.HasValue) query = query.Where(x => x.RequestTypeId == typeId); if (from.HasValue) query = query.Where(x => x.CreatedAt >= from); if (to.HasValue) query = query.Where(x => x.CreatedAt <= to); if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Title.Contains(search));
        var total = await query.CountAsync(ct); var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct); return (items, total);
    }
    public async Task AddApprovalAsync(Approval approval, CancellationToken ct) => await db.Approvals.AddAsync(approval, ct);
    public async Task AddCommentAsync(RequestComment comment, CancellationToken ct) => await db.RequestComments.AddAsync(comment, ct);
    public async Task AddAttachmentAsync(RequestAttachment attachment, CancellationToken ct) => await db.RequestAttachments.AddAsync(attachment, ct);
    public async Task AddNotificationAsync(Notification notification, CancellationToken ct) => await db.Notifications.AddAsync(notification, ct);
    public async Task<IReadOnlyList<Notification>> GetNotificationsAsync(int userId, int pageSize, bool unreadOnly, CancellationToken ct) => await db.Notifications.Where(x => x.UserId == userId && (!unreadOnly || !x.IsRead)).OrderByDescending(x => x.CreatedAt).Take(pageSize).ToListAsync(ct);
    public Task<int> GetUnreadNotificationCountAsync(int userId, CancellationToken ct) => db.Notifications.CountAsync(x => x.UserId == userId && !x.IsRead, ct);
    public Task<Notification?> FindNotificationAsync(int id, CancellationToken ct) => db.Notifications.FirstOrDefaultAsync(x => x.Id == id, ct);
    public async Task MarkAllNotificationsReadAsync(int userId, CancellationToken ct) => await db.Notifications.Where(x => x.UserId == userId && !x.IsRead).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsRead, true), ct);
    public async Task<IReadOnlyList<RequestHistory>> GetAllHistoryAsync(int page, int pageSize, CancellationToken ct) => await db.RequestHistory.Include(x => x.ChangedBy).Include(x => x.Request).OrderByDescending(x => x.ChangedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    public async Task SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
