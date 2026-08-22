using ERMS.Application.Common;
using ERMS.Application.DTOs;
using ERMS.Application.Interfaces;
using ERMS.Application.Mapping;
using ERMS.Domain.Entities;
using ERMS.Domain.Enums;
namespace ERMS.Application.Services;
/// <summary>
/// Talep yaşam döngüsünün sahibidir: oluşturma, taslak, gönderme, geri çekme,
/// yorum, ek ve görünürlük kuralları controller yerine burada uygulanır.
/// </summary>
public sealed class RequestService(IErmsRepository repository, IFileStorage files) : IRequestService
{
    /// <summary>[FR-16..24] Alanları doğrular, ilk durumu belirler ve ilk audit kaydını oluşturur.</summary>
    public async Task<RequestDetailDto> CreateAsync(int userId, CreateRequestDto dto, CancellationToken ct)
    {
        var type = await ValidateFieldsAsync(dto.RequestTypeId, dto.Title, dto.Description, dto.StartDate, dto.EndDate, dto.Amount, dto.SaveAsDraft, ct);
        var status = dto.SaveAsDraft ? RequestStatus.Draft : type.RequiresApproval ? RequestStatus.Pending : RequestStatus.Approved;
        var entity = new EmployeeRequest { RequesterId = userId, RequestTypeId = type.Id, RequestType = type, Title = dto.Title?.Trim() ?? "", Description = dto.Description?.Trim() ?? "", StartDate = ToUtc(dto.StartDate), EndDate = ToUtc(dto.EndDate), Amount = dto.Amount, Priority = dto.Priority, Status = status };
        entity.History.Add(new RequestHistory { ChangedById = userId, NewStatus = status, Action = status == RequestStatus.Draft ? "Talep taslak olarak oluşturuldu" : status == RequestStatus.Approved ? "Onay gerektirmeyen talep otomatik onaylandı" : "Talep oluşturuldu ve onaya gönderildi" });
        await repository.AddRequestAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        return (await repository.FindRequestAsync(entity.Id, true, ct))!.ToDetail();
    }

    public async Task<PagedResult<RequestListItem>> ListAsync(int userId, UserRole role, RequestStatus? status, int? typeId, DateTime? from, DateTime? to, string? search, int page, int pageSize, CancellationToken ct)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.SearchRequestsAsync(userId, role, status, typeId, ToUtc(from), ToUtc(to), search?.Trim(), page, pageSize, false, ct);
        return new(page, pageSize, total, items.Select(x => x.ToListItem()).ToList());
    }

    public async Task<RequestDetailDto> GetAsync(int userId, UserRole role, int id, CancellationToken ct)
        => (await GetAuthorizedAsync(userId, role, id, ct)).ToDetail();

    public async Task<RequestDetailDto> UpdateAsync(int userId, int id, UpdateRequestDto dto, CancellationToken ct)
    {
        var entity = await GetOwnedAsync(userId, id, ct);
        if (entity.Status != RequestStatus.Draft) throw new ConflictException("Yalnızca taslak talepler düzenlenebilir.");
        var type = await ValidateFieldsAsync(dto.RequestTypeId, dto.Title, dto.Description, dto.StartDate, dto.EndDate, dto.Amount, true, ct);
        entity.RequestTypeId = type.Id; entity.Title = dto.Title?.Trim() ?? ""; entity.Description = dto.Description?.Trim() ?? ""; entity.StartDate = ToUtc(dto.StartDate); entity.EndDate = ToUtc(dto.EndDate); entity.Amount = dto.Amount; entity.Priority = dto.Priority; entity.UpdatedAt = DateTime.UtcNow;
        await repository.SaveChangesAsync(ct);
        return (await repository.FindRequestAsync(id, true, ct))!.ToDetail();
    }

    public async Task<RequestDetailDto> SubmitAsync(int userId, int id, CancellationToken ct)
    {
        var entity = await GetOwnedAsync(userId, id, ct);
        if (entity.Status != RequestStatus.Draft) throw new ConflictException("Yalnızca taslak talepler gönderilebilir.");
        var type = await ValidateFieldsAsync(entity.RequestTypeId, entity.Title, entity.Description, entity.StartDate, entity.EndDate, entity.Amount, false, ct);
        var next = type.RequiresApproval ? RequestStatus.Pending : RequestStatus.Approved;
        ChangeStatus(entity, userId, next, next == RequestStatus.Pending ? "Taslak onaya gönderildi" : "Talep otomatik onaylandı");
        await repository.SaveChangesAsync(ct);
        return (await repository.FindRequestAsync(id, true, ct))!.ToDetail();
    }

    public async Task<RequestDetailDto> CancelAsync(int userId, int id, CancellationToken ct)
    {
        var entity = await GetOwnedAsync(userId, id, ct);
        if (entity.Status != RequestStatus.Pending) throw new ConflictException("Yalnızca bekleyen talepler geri çekilebilir; sonuçlanmış talepler iptal edilemez.");
        ChangeStatus(entity, userId, RequestStatus.Cancelled, "Talep sahibi tarafından geri çekildi");
        await repository.SaveChangesAsync(ct);
        return (await repository.FindRequestAsync(id, true, ct))!.ToDetail();
    }

    public async Task<CommentDto> AddCommentAsync(int userId, UserRole role, int id, CommentRequest dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Content)) throw new ValidationException("Yorum boş olamaz.", new Dictionary<string, string[]> { ["content"] = ["Yorum zorunludur."] });
        var entity = await GetAuthorizedAsync(userId, role, id, ct);
        // Admin talepleri denetim amacıyla okuyabilir; iş akışına yorumla müdahale etmez.
        if (role == UserRole.Admin) throw new ForbiddenException("Admin talep akışına yorum ekleyemez.");
        var content = dto.Content.Trim();
        var comment = new RequestComment { RequestId = entity.Id, AuthorId = userId, Content = content };
        await repository.AddCommentAsync(comment, ct);
        var recipientId = userId == entity.RequesterId ? entity.Requester?.ManagerId : entity.RequesterId;
        if (recipientId.HasValue && recipientId.Value != userId)
        {
            var author = await repository.FindUserAsync(userId, ct);
            await repository.AddNotificationAsync(new Notification
            {
                UserId = recipientId.Value,
                RequestId = entity.Id,
                Type = "Comment",
                Title = "Talebe yeni yorum eklendi",
                Message = $"{author?.FullName ?? "Bir kullanıcı"}: {(content.Length > 160 ? content[..160] + "…" : content)}"
            }, ct);
        }
        await repository.SaveChangesAsync(ct);
        var updated = await repository.FindRequestAsync(id, true, ct);
        return updated!.Comments.Single(x => x.Id == comment.Id) is var saved ? new(saved.Id, saved.Author?.FullName ?? "-", saved.Content, saved.CreatedAt) : throw new NotFoundException();
    }

    public async Task<AttachmentDto> AddAttachmentAsync(int userId, int id, FilePayload file, CancellationToken ct)
    {
        var entity = await GetOwnedAsync(userId, id, ct);
        if (file.Length <= 0 || file.Length > 10 * 1024 * 1024) throw new ValidationException("Dosya boyutu 10 MB sınırını aşamaz.");
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg" };
        if (!allowed.Contains(Path.GetExtension(file.FileName))) throw new ValidationException("Bu dosya türüne izin verilmiyor.");
        var storedName = await files.SaveAsync(file, ct);
        var attachment = new RequestAttachment { RequestId = entity.Id, UploadedById = userId, FileName = Path.GetFileName(file.FileName), StoredFileName = storedName, ContentType = file.ContentType, FileSize = file.Length };
        await repository.AddAttachmentAsync(attachment, ct); await repository.SaveChangesAsync(ct);
        return new(attachment.Id, attachment.FileName, attachment.ContentType, attachment.FileSize, attachment.CreatedAt);
    }

    public async Task<(RequestAttachment attachment, StoredFile file)> DownloadAttachmentAsync(int userId, UserRole role, int requestId, int attachmentId, CancellationToken ct)
    {
        var entity = await GetAuthorizedAsync(userId, role, requestId, ct);
        var attachment = entity.Attachments.SingleOrDefault(x => x.Id == attachmentId) ?? throw new NotFoundException("Dosya bulunamadı.");
        return (attachment, await files.OpenAsync(attachment.StoredFileName, attachment.FileName, ct));
    }

    private async Task<EmployeeRequest> GetOwnedAsync(int userId, int id, CancellationToken ct)
    {
        var entity = await repository.FindRequestAsync(id, true, ct) ?? throw new NotFoundException("Talep bulunamadı.");
        if (entity.RequesterId != userId) throw new ForbiddenException();
        return entity;
    }
    private async Task<EmployeeRequest> GetAuthorizedAsync(int userId, UserRole role, int id, CancellationToken ct)
    {
        var entity = await repository.FindRequestAsync(id, true, ct) ?? throw new NotFoundException("Talep bulunamadı.");
        if (role != UserRole.Admin && entity.RequesterId != userId && !(role == UserRole.Manager && entity.Requester?.ManagerId == userId)) throw new ForbiddenException();
        return entity;
    }
    private async Task<RequestType> ValidateFieldsAsync(int typeId, string? title, string? description, DateTime? start, DateTime? end, decimal? amount, bool draft, CancellationToken ct)
    {
        var type = await repository.FindRequestTypeAsync(typeId, ct) ?? throw new ValidationException("Talep türü bulunamadı.");
        if (!type.IsActive) throw new ValidationException("Pasif talep türü seçilemez.");
        if (!draft && (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))) throw new ValidationException("Zorunlu alanları doldurun.", new Dictionary<string, string[]> { ["title"] = ["Başlık zorunludur."], ["description"] = ["Açıklama zorunludur."] });
        if (!draft && type.Name.Equals("İzin", StringComparison.OrdinalIgnoreCase) && (start is null || end is null)) throw new ValidationException("İzin taleplerinde başlangıç ve bitiş tarihi zorunludur.");
        if (start.HasValue && end.HasValue && end < start) throw new ValidationException("Bitiş tarihi başlangıçtan önce olamaz.");
        if (!draft && type.Name.Equals("Masraf", StringComparison.OrdinalIgnoreCase) && (!amount.HasValue || amount <= 0)) throw new ValidationException("Masraf tutarı pozitif olmalıdır.");
        return type;
    }
    private static void ChangeStatus(EmployeeRequest entity, int actorId, RequestStatus status, string action) { var old = entity.Status; entity.Status = status; entity.UpdatedAt = DateTime.UtcNow; entity.History.Add(new RequestHistory { ChangedById = actorId, OldStatus = old, NewStatus = status, Action = action }); }
    private static (int, int) NormalizePaging(int page, int pageSize) => (Math.Max(page, 1), Math.Clamp(pageSize, 1, 100));
    private static DateTime? ToUtc(DateTime? date) => date is null ? null : date.Value.Kind == DateTimeKind.Utc ? date : date.Value.ToUniversalTime();
}
