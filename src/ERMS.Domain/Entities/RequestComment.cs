namespace ERMS.Domain.Entities;
public sealed class RequestComment
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public EmployeeRequest? Request { get; set; }
    public int AuthorId { get; set; }
    public User? Author { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
