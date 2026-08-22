namespace ERMS.Application.Common;
public abstract class AppException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
public sealed class ValidationException(string message, IDictionary<string, string[]>? errors = null) : AppException("VALIDATION_ERROR", message)
{
    public IDictionary<string, string[]> Errors { get; } = errors ?? new Dictionary<string, string[]>();
}
public sealed class UnauthorizedException(string message = "Oturum bilgileri geçersiz.") : AppException("UNAUTHORIZED", message);
public sealed class InvalidCredentialsException(string message = "E-posta veya parola hatalı.") : AppException("INVALID_CREDENTIALS", message);
public sealed class ForbiddenException(string message = "Bu işlem için yetkiniz yok.") : AppException("FORBIDDEN", message);
public sealed class NotFoundException(string message = "Kayıt bulunamadı.") : AppException("NOT_FOUND", message);
public sealed class ConflictException(string message) : AppException("CONFLICT", message);
