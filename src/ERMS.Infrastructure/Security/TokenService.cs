using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ERMS.Application.Interfaces;
using ERMS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
namespace ERMS.Infrastructure.Security;
/// <summary>JWT access token ve rastgele refresh token üretir.</summary>
public sealed class TokenService(IConfiguration configuration) : ITokenService
{
    /// <summary>Kullanıcı kimliği, rolü ve ilk-parola durumunu imzalı JWT claim'lerine koyar.</summary>
    public (string token, DateTime expiresAt) CreateAccessToken(User user)
    {
        var minutes = int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var configuredMinutes) ? configuredMinutes : 30;
        var expiresAt = DateTime.UtcNow.AddMinutes(minutes);
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.FullName), new Claim(ClaimTypes.Email, user.Email), new Claim(ClaimTypes.Role, user.Role.ToString()), new Claim("must_change_password", user.MustChangePassword ? "true" : "false") };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key tanımlanmalı.")));
        var jwt = new JwtSecurityToken(configuration["Jwt:Issuer"], configuration["Jwt:Audience"], claims, expires: expiresAt, signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expiresAt);
    }
    /// <summary>Kriptografik olarak güvenli, tahmin edilemez refresh token üretir.</summary>
    public string CreateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    /// <summary>Refresh token'ın kendisi DB'ye yazılmaz; yalnızca SHA-256 özeti tutulur.</summary>
    public string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
