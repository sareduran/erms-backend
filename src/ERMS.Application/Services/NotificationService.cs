using ERMS.Application.Common;
using ERMS.Application.DTOs;
using ERMS.Application.Interfaces;
namespace ERMS.Application.Services;
public sealed class NotificationService(IErmsRepository repository) : INotificationService
{
    public async Task<NotificationListDto> ListAsync(int userId, int pageSize, bool unreadOnly, CancellationToken ct)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var items = await repository.GetNotificationsAsync(userId, pageSize, unreadOnly, ct);
        var unreadCount = await repository.GetUnreadNotificationCountAsync(userId, ct);
        return new NotificationListDto(unreadCount, items.Select(x => new NotificationDto(x.Id, x.RequestId, x.Type, x.Title, x.Message, x.IsRead, x.CreatedAt)).ToList());
    }
    public async Task MarkReadAsync(int userId, int id, CancellationToken ct)
    {
        var item = await repository.FindNotificationAsync(id, ct) ?? throw new NotFoundException("Bildirim bulunamadı.");
        if (item.UserId != userId) throw new ForbiddenException();
        item.IsRead = true; await repository.SaveChangesAsync(ct);
    }
    public async Task MarkAllReadAsync(int userId, CancellationToken ct)
    {
        await repository.MarkAllNotificationsReadAsync(userId, ct);
        await repository.SaveChangesAsync(ct);
    }
}
