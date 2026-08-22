using ERMS.Application.DTOs;
using ERMS.Application.Interfaces;
using ERMS.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ERMS.Api.Controllers;
[ApiController, Route("api/auth")]
public sealed class AuthController(IAuthService service) : ControllerBase
{
    [AllowAnonymous, HttpPost("login")] public Task<AuthResponse> Login(LoginRequest request, CancellationToken ct) => service.LoginAsync(request, ct);
    [AllowAnonymous, HttpPost("refresh")] public Task<AuthResponse> Refresh(RefreshRequest request, CancellationToken ct) => service.RefreshAsync(request, ct);
    [Authorize, HttpPost("change-password")] public Task<AuthResponse> ChangePassword(ChangePasswordRequest request, CancellationToken ct) => service.ChangePasswordAsync(User.UserId(), request, ct);
}
