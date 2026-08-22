using ERMS.Api.Common;
using ERMS.Application.DTOs;
using ERMS.Application.Interfaces;
using ERMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
namespace ERMS.Api.Controllers;
[ApiController, Authorize, Route("api/requests")]
public sealed class RequestsController(IRequestService service) : ControllerBase
{
    [HttpPost, Authorize(Roles = "Employee,Manager"), EnableRateLimiting("request-creation")] public async Task<ActionResult<RequestDetailDto>> Create(CreateRequestDto dto, CancellationToken ct) { var result = await service.CreateAsync(User.UserId(), dto, ct); return CreatedAtAction(nameof(Get), new { id = result.Id }, result); }
    [HttpGet] public Task<PagedResult<RequestListItem>> List([FromQuery] RequestStatus? status, [FromQuery] int? typeId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default) => service.ListAsync(User.UserId(), User.UserRole(), status, typeId, from, to, search, page, pageSize, ct);
    [HttpGet("{id:int}")] public Task<RequestDetailDto> Get(int id, CancellationToken ct) => service.GetAsync(User.UserId(), User.UserRole(), id, ct);
    [HttpPut("{id:int}"), Authorize(Roles = "Employee,Manager")] public Task<RequestDetailDto> Update(int id, UpdateRequestDto dto, CancellationToken ct) => service.UpdateAsync(User.UserId(), id, dto, ct);
    [HttpPost("{id:int}/submit"), Authorize(Roles = "Employee,Manager")] public Task<RequestDetailDto> Submit(int id, CancellationToken ct) => service.SubmitAsync(User.UserId(), id, ct);
    [HttpPost("{id:int}/cancel"), Authorize(Roles = "Employee,Manager")] public Task<RequestDetailDto> Cancel(int id, CancellationToken ct) => service.CancelAsync(User.UserId(), id, ct);
    [HttpPost("{id:int}/comments")] public Task<CommentDto> Comment(int id, CommentRequest dto, CancellationToken ct) => service.AddCommentAsync(User.UserId(), User.UserRole(), id, dto, ct);
    [HttpPost("{id:int}/attachments"), RequestSizeLimit(10 * 1024 * 1024)] public async Task<AttachmentDto> Upload(int id, IFormFile file, CancellationToken ct) { await using var stream = file.OpenReadStream(); return await service.AddAttachmentAsync(User.UserId(), id, new FilePayload(file.FileName, file.ContentType, stream, file.Length), ct); }
    [HttpGet("{requestId:int}/attachments/{attachmentId:int}")] public async Task<IActionResult> Download(int requestId, int attachmentId, CancellationToken ct) { var (attachment, stored) = await service.DownloadAttachmentAsync(User.UserId(), User.UserRole(), requestId, attachmentId, ct); return File(stored.Content, attachment.ContentType, attachment.FileName); }
}
