using ERMS.Application.Interfaces;
namespace ERMS.Infrastructure.Security;
/// <summary>
/// Parolayı geri çözülebilen biçimde şifrelemez; BCrypt ile tek yönlü hash üretir.
/// Aynı parola her hash işleminde farklı salt aldığı için veritabanındaki değerler de farklı görünür.
/// </summary>
public sealed class PasswordService : IPasswordService
{
    /// <summary>Work factor 12, deneme-yanılma saldırılarını yavaşlatır.</summary>
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    /// <summary>Girişte düz parolayı saklamadan BCrypt hash'iyle karşılaştırır.</summary>
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
