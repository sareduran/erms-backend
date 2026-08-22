using ERMS.Application.DTOs;
using ERMS.Domain.Entities;
using ERMS.Domain.Enums;
namespace ERMS.Application.Interfaces;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ITokenService
{
    (string token, DateTime expiresAt) CreateAccessToken(User user);
    string CreateRefreshToken();
    string HashToken(string token);
}

public interface IFileStorage
{
    Task<string> SaveAsync(FilePayload file, CancellationToken cancellationToken);
    Task<StoredFile> OpenAsync(string storedFileName, string originalFileName, CancellationToken cancellationToken);
}

public interface IErmsRepository
{
    Task<User?> FindUserByEmailAsync(string email, CancellationToken ct);
    Task<User?> FindUserAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken ct);
    Task AddUserAsync(User user, CancellationToken ct);
    Task<IReadOnlyList<Department>> GetDepartmentsAsync(bool activeOnly, CancellationToken ct);
    Task<Department?> FindDepartmentAsync(int id, CancellationToken ct);
    Task AddDepartmentAsync(Department department, CancellationToken ct);
    Task<IReadOnlyList<RequestType>> GetRequestTypesAsync(bool activeOnly, CancellationToken ct);
    Task<RequestType?> FindRequestTypeAsync(int id, CancellationToken ct);
    Task AddRequestTypeAsync(RequestType requestType, CancellationToken ct);
    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct);
    Task<RefreshToken?> FindRefreshTokenAsync(string tokenHash, CancellationToken ct);
    Task RevokeRefreshTokensAsync(int userId, CancellationToken ct);
    Task<EmployeeRequest?> FindRequestAsync(int id, bool includeDetails, CancellationToken ct);
    Task AddRequestAsync(EmployeeRequest request, CancellationToken ct);
    Task<(IReadOnlyList<EmployeeRequest> items, int total)> SearchRequestsAsync(int actorId, UserRole role, RequestStatus? status, int? typeId, DateTime? from, DateTime? to, string? search, int page, int pageSize, bool pendingForManager, CancellationToken ct);
    Task AddApprovalAsync(Approval approval, CancellationToken ct);
    Task AddCommentAsync(RequestComment comment, CancellationToken ct);
    Task AddAttachmentAsync(RequestAttachment attachment, CancellationToken ct);
    Task AddNotificationAsync(Notification notification, CancellationToken ct);
    Task<IReadOnlyList<Notification>> GetNotificationsAsync(int userId, int pageSize, bool unreadOnly, CancellationToken ct);
    Task<int> GetUnreadNotificationCountAsync(int userId, CancellationToken ct);
    Task<Notification?> FindNotificationAsync(int id, CancellationToken ct);
    Task MarkAllNotificationsReadAsync(int userId, CancellationToken ct);
    Task<IReadOnlyList<RequestHistory>> GetAllHistoryAsync(int page, int pageSize, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct);
    Task<AuthResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct);
}
public interface IRequestService
{
    Task<RequestDetailDto> CreateAsync(int userId, CreateRequestDto dto, CancellationToken ct);
    Task<PagedResult<RequestListItem>> ListAsync(int userId, UserRole role, RequestStatus? status, int? typeId, DateTime? from, DateTime? to, string? search, int page, int pageSize, CancellationToken ct);
    Task<RequestDetailDto> GetAsync(int userId, UserRole role, int id, CancellationToken ct);
    Task<RequestDetailDto> UpdateAsync(int userId, int id, UpdateRequestDto dto, CancellationToken ct);
    Task<RequestDetailDto> SubmitAsync(int userId, int id, CancellationToken ct);
    Task<RequestDetailDto> CancelAsync(int userId, int id, CancellationToken ct);
    Task<CommentDto> AddCommentAsync(int userId, UserRole role, int id, CommentRequest dto, CancellationToken ct);
    Task<AttachmentDto> AddAttachmentAsync(int userId, int id, FilePayload file, CancellationToken ct);
    Task<(RequestAttachment attachment, StoredFile file)> DownloadAttachmentAsync(int userId, UserRole role, int requestId, int attachmentId, CancellationToken ct);
}
/// <summary>Aktif talep türlerini kullanıcı ekranına DTO olarak sunar.</summary>
public interface IRequestTypeService
{
    Task<IReadOnlyList<RequestTypeDto>> ListActiveAsync(CancellationToken ct);
}
public interface IApprovalService
{
    Task<PagedResult<RequestListItem>> PendingAsync(int managerId, int page, int pageSize, CancellationToken ct);
    Task<RequestDetailDto> DecideAsync(int managerId, int requestId, bool approve, DecisionRequest dto, CancellationToken ct);
}
public interface IAdminService
{
    Task<IReadOnlyList<UserSummary>> UsersAsync(CancellationToken ct);
    Task<UserSummary> CreateUserAsync(CreateUserRequest dto, CancellationToken ct);
    Task<UserSummary> UpdateUserAsync(int id, UpdateUserRequest dto, CancellationToken ct);
    Task<IReadOnlyList<DepartmentDto>> DepartmentsAsync(CancellationToken ct);
    Task<DepartmentDto> CreateDepartmentAsync(UpsertDepartmentRequest dto, CancellationToken ct);
    Task<DepartmentDto> UpdateDepartmentAsync(int id, UpsertDepartmentRequest dto, CancellationToken ct);
    Task<IReadOnlyList<RequestTypeDto>> RequestTypesAsync(CancellationToken ct);
    Task<RequestTypeDto> CreateRequestTypeAsync(UpsertRequestTypeRequest dto, CancellationToken ct);
    Task<RequestTypeDto> UpdateRequestTypeAsync(int id, UpsertRequestTypeRequest dto, CancellationToken ct);
    Task<IReadOnlyList<HistoryDto>> HistoryAsync(int page, int pageSize, CancellationToken ct);
}
public interface INotificationService
{
    Task<NotificationListDto> ListAsync(int userId, int pageSize, bool unreadOnly, CancellationToken ct);
    Task MarkReadAsync(int userId, int id, CancellationToken ct);
    Task MarkAllReadAsync(int userId, CancellationToken ct);
}
