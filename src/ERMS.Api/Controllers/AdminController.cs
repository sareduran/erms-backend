using ERMS.Application.DTOs;
using ERMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ERMS.Api.Controllers;
[ApiController, Authorize(Roles = "Admin"), Route("api/admin")]
public sealed class AdminController(IAdminService service) : ControllerBase
{
    [HttpGet("users")] public Task<IReadOnlyList<UserSummary>> Users(CancellationToken ct) => service.UsersAsync(ct);
    [HttpPost("users")] public Task<UserSummary> CreateUser(CreateUserRequest dto, CancellationToken ct) => service.CreateUserAsync(dto, ct);
    [HttpPut("users/{id:int}")] public Task<UserSummary> UpdateUser(int id, UpdateUserRequest dto, CancellationToken ct) => service.UpdateUserAsync(id, dto, ct);
    [HttpGet("departments")] public Task<IReadOnlyList<DepartmentDto>> Departments(CancellationToken ct) => service.DepartmentsAsync(ct);
    [HttpPost("departments")] public Task<DepartmentDto> CreateDepartment(UpsertDepartmentRequest dto, CancellationToken ct) => service.CreateDepartmentAsync(dto, ct);
    [HttpPut("departments/{id:int}")] public Task<DepartmentDto> UpdateDepartment(int id, UpsertDepartmentRequest dto, CancellationToken ct) => service.UpdateDepartmentAsync(id, dto, ct);
    [HttpGet("request-types")] public Task<IReadOnlyList<RequestTypeDto>> Types(CancellationToken ct) => service.RequestTypesAsync(ct);
    [HttpPost("request-types")] public Task<RequestTypeDto> CreateType(UpsertRequestTypeRequest dto, CancellationToken ct) => service.CreateRequestTypeAsync(dto, ct);
    [HttpPut("request-types/{id:int}")] public Task<RequestTypeDto> UpdateType(int id, UpsertRequestTypeRequest dto, CancellationToken ct) => service.UpdateRequestTypeAsync(id, dto, ct);
    [HttpGet("history")] public Task<IReadOnlyList<HistoryDto>> History([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) => service.HistoryAsync(page, pageSize, ct);
}
