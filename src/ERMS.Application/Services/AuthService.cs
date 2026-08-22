using ERMS.Application.Common;
using ERMS.Application.DTOs;
using ERMS.Application.Interfaces;
using ERMS.Application.Mapping;
using ERMS.Domain.Entities;
namespace ERMS.Application.Services;
/// <summary>Giriş, token yenileme ve ilk girişte parola değiştirme kullanım senaryolarını yürütür.</summary>
public sealed class AuthService(IErmsRepository repository, IPasswordService passwords, ITokenService tokens) : IAuthService
{
    /// <summary>[FR-01/02/03/08] Kullanıcıyı BCrypt ile doğrular ve aktif hesaba token verir.</summary>
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await repository.FindUserByEmailAsync(email, ct);
        if (user is null || !user.IsActive || !passwords.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();
        return await IssueAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var existing = await repository.FindRefreshTokenAsync(tokens.HashToken(request.RefreshToken), ct);
        if (existing is null || !existing.IsActive || existing.User is null || !existing.User.IsActive)
            throw new UnauthorizedException("Yenileme tokenı geçersiz veya süresi dolmuş.");
        existing.RevokedAt = DateTime.UtcNow;
        return await IssueAsync(existing.User, ct);
    }

    public async Task<AuthResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await repository.FindUserAsync(userId, ct);
        if (user is null || !user.IsActive) throw new UnauthorizedException("Kullanıcı hesabı bulunamadı veya pasif.");
        if (!passwords.Verify(request.CurrentPassword, user.PasswordHash))
            throw new ValidationException("Mevcut parola hatalı.", new Dictionary<string, string[]> { ["currentPassword"] = ["Mevcut parola hatalı."] });
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new ValidationException("Yeni parola en az 8 karakter olmalıdır.", new Dictionary<string, string[]> { ["newPassword"] = ["Yeni parola en az 8 karakter olmalıdır."] });
        if (passwords.Verify(request.NewPassword, user.PasswordHash))
            throw new ValidationException("Yeni parola geçici paroladan farklı olmalıdır.", new Dictionary<string, string[]> { ["newPassword"] = ["Farklı bir parola belirleyin."] });

        user.PasswordHash = passwords.Hash(request.NewPassword);
        user.MustChangePassword = false;
        await repository.RevokeRefreshTokensAsync(user.Id, ct);
        return await IssueAsync(user, ct);
    }

    private async Task<AuthResponse> IssueAsync(User user, CancellationToken ct)
    {
        // Access token kısa ömürlüdür. Refresh token ise istemciye ham hâliyle yalnızca
        // bu yanıtta verilir; repository'ye SHA-256 özeti gönderilir.
        var (accessToken, expiresAt) = tokens.CreateAccessToken(user);
        var rawRefresh = tokens.CreateRefreshToken();
        await repository.AddRefreshTokenAsync(new RefreshToken { UserId = user.Id, TokenHash = tokens.HashToken(rawRefresh), ExpiresAt = DateTime.UtcNow.AddDays(7) }, ct);
        await repository.SaveChangesAsync(ct);
        return new AuthResponse(accessToken, rawRefresh, expiresAt, user.ToSummary());
    }
}
