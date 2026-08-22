namespace ERMS.Domain.Entities;
public sealed class RequestAttachment
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public EmployeeRequest? Request { get; set; }
    public int UploadedById { get; set; }
    public User? UploadedBy { get; set; }
    public required string FileName { get; set; }
    public required string StoredFileName { get; set; }
    public required string ContentType { get; set; }
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
