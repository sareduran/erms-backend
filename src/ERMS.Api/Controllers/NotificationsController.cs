using ERMS.Api.Common;
using ERMS.Application.DTOs;
using ERMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ERMS.Api.Controllers;
[ApiController, Authorize, Route("api/notifications")]
public sealed class NotificationsController(INotificationService service) : ControllerBase
{
    [HttpGet] public Task<NotificationListDto> List([FromQuery] int pageSize = 20, [FromQuery] bool unreadOnly = false, CancellationToken ct = default) => service.ListAsync(User.UserId(), pageSize, unreadOnly, ct);
    [HttpPost("{id:int}/read")] public async Task<IActionResult> MarkRead(int id, CancellationToken ct) { await service.MarkReadAsync(User.UserId(), id, ct); return NoContent(); }
    [HttpPost("read-all")] public async Task<IActionResult> MarkAllRead(CancellationToken ct) { await service.MarkAllReadAsync(User.UserId(), ct); return NoContent(); }
}
