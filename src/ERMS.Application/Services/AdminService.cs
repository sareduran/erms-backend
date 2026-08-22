using ERMS.Application.Common;
using ERMS.Application.DTOs;
using ERMS.Application.Interfaces;
using ERMS.Application.Mapping;
using ERMS.Domain.Entities;
namespace ERMS.Application.Services;
/// <summary>Kullanıcı, departman ve talep türü gibi değişken sistem tanımlarını yönetir.</summary>
public sealed class AdminService(IErmsRepository repository, IPasswordService passwords) : IAdminService
{
    public async Task<IReadOnlyList<UserSummary>> UsersAsync(CancellationToken ct) => (await repository.GetUsersAsync(ct)).Select(x => x.ToSummary()).ToList();
    public async Task<UserSummary> CreateUserAsync(CreateUserRequest dto, CancellationToken ct)
    {
        ValidatePersonName(dto.FirstName, dto.LastName);
        await ValidateUserAsync(dto.Email, dto.DepartmentId, dto.ManagerId, dto.Role, null, ct);
        if (dto.Password.Length < 8) throw new ValidationException("Parola en az 8 karakter olmalıdır.");
        var user = new User { FirstName = dto.FirstName.Trim(), LastName = dto.LastName.Trim(), Email = NormalizeEmail(dto.Email), PasswordHash = passwords.Hash(dto.Password), Role = dto.Role, DepartmentId = dto.DepartmentId, ManagerId = dto.ManagerId, MustChangePassword = true };
        await repository.AddUserAsync(user, ct); await repository.SaveChangesAsync(ct);
        return (await repository.FindUserAsync(user.Id, ct))!.ToSummary();
    }
    public async Task<UserSummary> UpdateUserAsync(int id, UpdateUserRequest dto, CancellationToken ct)
    {
        var user = await repository.FindUserAsync(id, ct) ?? throw new NotFoundException("Kullanıcı bulunamadı.");
        ValidatePersonName(dto.FirstName, dto.LastName);
        await ValidateUserAsync(dto.Email, dto.DepartmentId, dto.ManagerId, dto.Role, id, ct);
        user.FirstName = dto.FirstName.Trim(); user.LastName = dto.LastName.Trim(); user.Email = NormalizeEmail(dto.Email); user.Role = dto.Role; user.DepartmentId = dto.DepartmentId; user.ManagerId = dto.ManagerId; user.IsActive = dto.IsActive;
        if (!string.IsNullOrWhiteSpace(dto.NewPassword)) { if (dto.NewPassword.Length < 8) throw new ValidationException("Parola en az 8 karakter olmalıdır."); user.PasswordHash = passwords.Hash(dto.NewPassword); user.MustChangePassword = true; }
        await repository.SaveChangesAsync(ct); return (await repository.FindUserAsync(id, ct))!.ToSummary();
    }
    public async Task<IReadOnlyList<DepartmentDto>> DepartmentsAsync(CancellationToken ct) => (await repository.GetDepartmentsAsync(false, ct)).Select(x => new DepartmentDto(x.Id, x.Name, x.IsActive)).ToList();
    public async Task<DepartmentDto> CreateDepartmentAsync(UpsertDepartmentRequest dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new ValidationException("Departman adı zorunludur.");
        var item = new Department { Name = dto.Name.Trim(), IsActive = dto.IsActive }; await repository.AddDepartmentAsync(item, ct); await repository.SaveChangesAsync(ct); return new(item.Id, item.Name, item.IsActive);
    }
    public async Task<DepartmentDto> UpdateDepartmentAsync(int id, UpsertDepartmentRequest dto, CancellationToken ct)
    {
        var item = await repository.FindDepartmentAsync(id, ct) ?? throw new NotFoundException("Departman bulunamadı."); item.Name = dto.Name.Trim(); item.IsActive = dto.IsActive; await repository.SaveChangesAsync(ct); return new(item.Id, item.Name, item.IsActive);
    }
    public async Task<IReadOnlyList<RequestTypeDto>> RequestTypesAsync(CancellationToken ct) => (await repository.GetRequestTypesAsync(false, ct)).Select(x => new RequestTypeDto(x.Id, x.Name, x.RequiresApproval, x.IsActive)).ToList();
    public async Task<RequestTypeDto> CreateRequestTypeAsync(UpsertRequestTypeRequest dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new ValidationException("Talep türü adı zorunludur.");
        var item = new RequestType { Name = dto.Name.Trim(), RequiresApproval = dto.RequiresApproval, IsActive = dto.IsActive }; await repository.AddRequestTypeAsync(item, ct); await repository.SaveChangesAsync(ct); return new(item.Id, item.Name, item.RequiresApproval, item.IsActive);
    }
    public async Task<RequestTypeDto> UpdateRequestTypeAsync(int id, UpsertRequestTypeRequest dto, CancellationToken ct)
    {
        var item = await repository.FindRequestTypeAsync(id, ct) ?? throw new NotFoundException("Talep türü bulunamadı."); item.Name = dto.Name.Trim(); item.RequiresApproval = dto.RequiresApproval; item.IsActive = dto.IsActive; await repository.SaveChangesAsync(ct); return new(item.Id, item.Name, item.RequiresApproval, item.IsActive);
    }
    public async Task<IReadOnlyList<HistoryDto>> HistoryAsync(int page, int pageSize, CancellationToken ct) => (await repository.GetAllHistoryAsync(Math.Max(page, 1), Math.Clamp(pageSize, 1, 100), ct)).Select(x => new HistoryDto(x.Id, x.RequestId, x.OldStatus?.ToString(), x.NewStatus.ToString(), x.Action, x.ChangedBy?.FullName ?? "-", x.ChangedAt)).ToList();

    private async Task ValidateUserAsync(string email, int departmentId, int? managerId, Domain.Enums.UserRole role, int? currentId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.EndsWith("@oyakdijital.com", StringComparison.OrdinalIgnoreCase)) throw new ValidationException("Kurumsal e-posta @oyakdijital.com uzantılı olmalıdır.");
        var duplicate = await repository.FindUserByEmailAsync(NormalizeEmail(email), ct); if (duplicate is not null && duplicate.Id != currentId) throw new ConflictException("Bu e-posta zaten kullanımda.");
        if ((await repository.FindDepartmentAsync(departmentId, ct)) is not { IsActive: true }) throw new ValidationException("Geçerli ve aktif bir departman seçin.");
        if (managerId.HasValue) { var manager = await repository.FindUserAsync(managerId.Value, ct); if (manager is null || !manager.IsActive || manager.Role != Domain.Enums.UserRole.Manager) throw new ValidationException("Seçilen yönetici geçerli değil."); if (manager.Id == currentId) throw new ValidationException("Kullanıcı kendi yöneticisi olamaz."); }
        if (role == Domain.Enums.UserRole.Employee && !managerId.HasValue) throw new ValidationException("Çalışan için yönetici seçimi zorunludur.");
        if (role == Domain.Enums.UserRole.Admin && managerId.HasValue) throw new ValidationException("Admin kullanıcı onay hiyerarşisine bağlanamaz.");
    }
    private static void ValidatePersonName(string firstName, string lastName)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(firstName)) errors["firstName"] = ["Ad zorunludur."];
        if (string.IsNullOrWhiteSpace(lastName)) errors["lastName"] = ["Soyad zorunludur."];
        if (errors.Count > 0) throw new ValidationException("Ad ve soyad alanlarını doldurun.", errors);
    }
    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
