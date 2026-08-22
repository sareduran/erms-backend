using System.Security.Claims;
using ERMS.Domain.Enums;
namespace ERMS.Api.Common;
public static class ClaimsPrincipalExtensions
{
    public static int UserId(this ClaimsPrincipal principal) => int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException());
    public static UserRole UserRole(this ClaimsPrincipal principal) => Enum.Parse<UserRole>(principal.FindFirstValue(ClaimTypes.Role) ?? throw new UnauthorizedAccessException());
}
