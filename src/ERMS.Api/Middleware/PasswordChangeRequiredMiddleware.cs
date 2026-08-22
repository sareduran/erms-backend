using ERMS.Application.Common;

namespace ERMS.Api.Middleware;

public sealed class PasswordChangeRequiredMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var mustChangePassword = context.User.Identity?.IsAuthenticated == true
            && string.Equals(context.User.FindFirst("must_change_password")?.Value, "true", StringComparison.OrdinalIgnoreCase);
        var allowedAuthPath = context.Request.Path.StartsWithSegments("/api/auth/change-password")
            || context.Request.Path.StartsWithSegments("/api/auth/refresh");

        if (mustChangePassword && !allowedAuthPath)
            throw new ForbiddenException("Devam etmeden önce geçici parolanızı değiştirmeniz gerekiyor.");

        await next(context);
    }
}
