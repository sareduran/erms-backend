namespace ERMS.Domain.Entities;
public sealed class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public required string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}
