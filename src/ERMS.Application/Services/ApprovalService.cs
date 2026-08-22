using ERMS.Application.Common;
using ERMS.Application.DTOs;
using ERMS.Application.Interfaces;
using ERMS.Application.Mapping;
using ERMS.Domain.Entities;
using ERMS.Domain.Enums;
namespace ERMS.Application.Services;
/// <summary>Yöneticinin yalnızca doğrudan bağlı çalışan taleplerinde karar verebilmesini sağlar.</summary>
public sealed class ApprovalService(IErmsRepository repository) : IApprovalService
{
    public async Task<PagedResult<RequestListItem>> PendingAsync(int managerId, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await repository.SearchRequestsAsync(managerId, UserRole.Manager, RequestStatus.Pending, null, null, null, null, page, pageSize, true, ct);
        return new(page, pageSize, total, items.Select(x => x.ToListItem()).ToList());
    }

    public async Task<RequestDetailDto> DecideAsync(int managerId, int requestId, bool approve, DecisionRequest dto, CancellationToken ct)
    {
        // Kontrollerin sırası sunumda önemlidir: sahiplik/hiyerarşi -> durum -> red gerekçesi.
        // Böylece bir yönetici kendi talebini veya başka ekibin talebini sonuçlandıramaz.
        var entity = await repository.FindRequestAsync(requestId, true, ct) ?? throw new NotFoundException("Talep bulunamadı.");
        if (entity.RequesterId == managerId) throw new ForbiddenException("Kendi talebinizi onaylayamazsınız.");
        if (entity.Requester?.ManagerId != managerId) throw new ForbiddenException("Yalnızca bağlı çalışanlarınızın taleplerini sonuçlandırabilirsiniz.");
        if (entity.Status != RequestStatus.Pending) throw new ConflictException("Yalnızca bekleyen talepler sonuçlandırılabilir.");
        if (!approve && string.IsNullOrWhiteSpace(dto.Comment)) throw new ValidationException("Red gerekçesi zorunludur.", new Dictionary<string, string[]> { ["comment"] = ["Red gerekçesi zorunludur."] });
        var old = entity.Status;
        entity.Status = approve ? RequestStatus.Approved : RequestStatus.Rejected;
        entity.UpdatedAt = DateTime.UtcNow;
        await repository.AddApprovalAsync(new Approval { RequestId = requestId, ApproverId = managerId, Decision = approve ? ApprovalDecision.Approved : ApprovalDecision.Rejected, Comment = dto.Comment?.Trim() }, ct);
        entity.History.Add(new RequestHistory { ChangedById = managerId, OldStatus = old, NewStatus = entity.Status, Action = approve ? "Talep yönetici tarafından onaylandı" : "Talep yönetici tarafından reddedildi" });
        await repository.AddNotificationAsync(new Notification
        {
            UserId = entity.RequesterId,
            RequestId = entity.Id,
            Type = approve ? "Approval" : "Rejection",
            Title = approve ? "Talebiniz onaylandı" : "Talebiniz reddedildi",
            Message = approve
                ? $"{entity.Title} talebiniz yönetici tarafından onaylandı."
                : $"{entity.Title} talebiniz reddedildi. Gerekçe: {dto.Comment?.Trim()}"
        }, ct);
        await repository.SaveChangesAsync(ct);
        return (await repository.FindRequestAsync(requestId, true, ct))!.ToDetail();
    }
}
