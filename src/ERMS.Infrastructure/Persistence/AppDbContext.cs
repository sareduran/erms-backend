using ERMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace ERMS.Infrastructure.Persistence;
/// <summary>
/// EF Core'un tabloları ve ilişkileri nasıl kuracağını tanımlar. İş kuralları burada değil,
/// Application servislerindedir; bu sınıf yalnızca veri eşlemesine odaklanır.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<RequestType> RequestTypes => Set<RequestType>();
    public DbSet<EmployeeRequest> Requests => Set<EmployeeRequest>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<RequestComment> RequestComments => Set<RequestComment>();
    public DbSet<RequestAttachment> RequestAttachments => Set<RequestAttachment>();
    public DbSet<RequestHistory> RequestHistory => Set<RequestHistory>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(x => x.Email).IsUnique(); entity.Property(x => x.Email).HasMaxLength(256); entity.Property(x => x.FirstName).HasMaxLength(100); entity.Property(x => x.LastName).HasMaxLength(100); entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(x => x.Manager).WithMany(x => x.DirectReports).HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Department).WithMany(x => x.Users).HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<EmployeeRequest>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20); entity.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20); entity.Property(x => x.Amount).HasPrecision(18, 2); entity.Property(x => x.Title).HasMaxLength(200);
            entity.HasOne(x => x.Requester).WithMany().HasForeignKey(x => x.RequesterId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RequestType).WithMany().HasForeignKey(x => x.RequestTypeId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Approval>().Property(x => x.Decision).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Approval>().HasOne(x => x.Approver).WithMany().HasForeignKey(x => x.ApproverId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RequestComment>().HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RequestAttachment>().HasOne(x => x.UploadedBy).WithMany().HasForeignKey(x => x.UploadedById).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RequestHistory>().Property(x => x.OldStatus).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<RequestHistory>().Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<RequestHistory>().HasOne(x => x.ChangedBy).WithMany().HasForeignKey(x => x.ChangedById).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RequestType>().HasIndex(x => x.Name).IsUnique(); modelBuilder.Entity<Department>().HasIndex(x => x.Name).IsUnique(); modelBuilder.Entity<RefreshToken>().HasIndex(x => x.TokenHash).IsUnique();
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(x => x.Type).HasMaxLength(30); entity.Property(x => x.Title).HasMaxLength(160); entity.Property(x => x.Message).HasMaxLength(500);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
        });
    }
}
