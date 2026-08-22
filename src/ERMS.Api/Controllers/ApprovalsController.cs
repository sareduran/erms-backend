using ERMS.Api.Common;
using ERMS.Application.DTOs;
using ERMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ERMS.Api.Controllers;
[ApiController, Authorize(Roles = "Manager"), Route("api/approvals")]
public sealed class ApprovalsController(IApprovalService service) : ControllerBase
{
    [HttpGet("pending")] public Task<PagedResult<RequestListItem>> Pending([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default) => service.PendingAsync(User.UserId(), page, pageSize, ct);
    [HttpPost("{requestId:int}/approve")] public Task<RequestDetailDto> Approve(int requestId, DecisionRequest dto, CancellationToken ct) => service.DecideAsync(User.UserId(), requestId, true, dto, ct);
    [HttpPost("{requestId:int}/reject")] public Task<RequestDetailDto> Reject(int requestId, DecisionRequest dto, CancellationToken ct) => service.DecideAsync(User.UserId(), requestId, false, dto, ct);
}
