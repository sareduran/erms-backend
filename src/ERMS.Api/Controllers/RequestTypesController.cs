using ERMS.Application.DTOs;
using ERMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ERMS.Api.Controllers;
[ApiController, Authorize, Route("api/request-types")]
public sealed class RequestTypesController(IRequestTypeService service) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<RequestTypeDto>> Get(CancellationToken ct) => service.ListActiveAsync(ct);
}
