namespace ERMS.Domain.Entities;
public sealed class RequestType
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool RequiresApproval { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
