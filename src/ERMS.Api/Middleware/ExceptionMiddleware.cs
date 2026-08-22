using System.Text.Json;
using ERMS.Application.Common;
namespace ERMS.Api.Middleware;
public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception)
        {
            var (status, code, message, errors) = exception switch
            {
                ValidationException x => (400, x.Code, x.Message, (object?)x.Errors),
                InvalidCredentialsException x => (401, x.Code, x.Message, null),
                UnauthorizedException x => (401, x.Code, x.Message, null),
                ForbiddenException x => (403, x.Code, x.Message, null),
                NotFoundException x => (404, x.Code, x.Message, null),
                ConflictException x => (409, x.Code, x.Message, null),
                _ => (500, "INTERNAL_ERROR", "Beklenmeyen bir sunucu hatası oluştu.", null)
            };
            if (status == 500) logger.LogError(exception, "Unhandled API error"); else logger.LogWarning(exception, "Handled API error: {Code}", code);
            context.Response.StatusCode = status; context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { code, message, errors }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        }
    }
}
