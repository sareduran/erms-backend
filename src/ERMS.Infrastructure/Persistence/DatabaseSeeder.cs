using ERMS.Application.Interfaces;
using ERMS.Domain.Entities;
using ERMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Data;
namespace ERMS.Infrastructure.Persistence;
public static class DatabaseSeeder
{
    private const string InitialPasswordVariable = "ERMS_DEMO_INITIAL_PASSWORD";

    public static async Task SeedAsync(AppDbContext db, IPasswordService passwords, CancellationToken ct = default)
    {
        if (db.Database.IsSqlite())
        {
            await db.Database.EnsureCreatedAsync(ct);
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS "Notifications" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Notifications" PRIMARY KEY AUTOINCREMENT,
                    "UserId" INTEGER NOT NULL,
                    "RequestId" INTEGER NULL,
                    "Type" TEXT NOT NULL,
                    "Title" TEXT NOT NULL,
                    "Message" TEXT NOT NULL,
                    "IsRead" INTEGER NOT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    CONSTRAINT "FK_Notifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_Notifications_Requests_RequestId" FOREIGN KEY ("RequestId") REFERENCES "Requests" ("Id") ON DELETE SET NULL
                );
                CREATE INDEX IF NOT EXISTS "IX_Notifications_RequestId" ON "Notifications" ("RequestId");
                CREATE INDEX IF NOT EXISTS "IX_Notifications_UserId_IsRead_CreatedAt" ON "Notifications" ("UserId", "IsRead", "CreatedAt");
                """, ct);
            await EnsureMustChangePasswordColumnAsync(db, ct);
        }
        else await db.Database.MigrateAsync(ct);
        if (await db.Users.AnyAsync(ct))
        {
            var managerOne = await db.Users.FirstOrDefaultAsync(x => x.Email == "mehmet.demir@oyakdijital.com", ct);
            if (managerOne is not null && managerOne.ManagerId is null)
            {
                var existingManagerTwo = await db.Users.FirstOrDefaultAsync(x => x.Email == "selin.kaya@oyakdijital.com", ct);
                if (existingManagerTwo is null)
                {
                    existingManagerTwo = new User { FirstName = "Selin", LastName = "Kaya", Email = "selin.kaya@oyakdijital.com", PasswordHash = passwords.Hash(GetInitialPassword()), Role = UserRole.Manager, DepartmentId = managerOne.DepartmentId };
                    db.Users.Add(existingManagerTwo);
                    await db.SaveChangesAsync(ct);
                }
                managerOne.ManagerId = existingManagerTwo.Id;
                await db.SaveChangesAsync(ct);
            }
            return;
        }
        var technology = new Department { Name = "Teknoloji" }; var finance = new Department { Name = "Finans" }; var hr = new Department { Name = "İnsan Kaynakları" };
        db.Departments.AddRange(technology, finance, hr); await db.SaveChangesAsync(ct);
        var initialPassword = GetInitialPassword();
        var managerTwo = new User { FirstName = "Selin", LastName = "Kaya", Email = "selin.kaya@oyakdijital.com", PasswordHash = passwords.Hash(initialPassword), Role = UserRole.Manager, DepartmentId = technology.Id };
        var admin = new User { FirstName = "Ayşe", LastName = "Kaya", Email = "ayse.kaya@oyakdijital.com", PasswordHash = passwords.Hash(initialPassword), Role = UserRole.Admin, DepartmentId = technology.Id };
        db.Users.AddRange(managerTwo, admin); await db.SaveChangesAsync(ct);
        var manager = new User { FirstName = "Mehmet", LastName = "Demir", Email = "mehmet.demir@oyakdijital.com", PasswordHash = passwords.Hash(initialPassword), Role = UserRole.Manager, DepartmentId = technology.Id, ManagerId = managerTwo.Id };
        db.Users.Add(manager); await db.SaveChangesAsync(ct);
        var employee = new User { FirstName = "Ahmet", LastName = "Yılmaz", Email = "ahmet.yilmaz@oyakdijital.com", PasswordHash = passwords.Hash(initialPassword), Role = UserRole.Employee, DepartmentId = technology.Id, ManagerId = manager.Id };
        db.Users.Add(employee);
        db.RequestTypes.AddRange(new RequestType { Name = "İzin", RequiresApproval = true }, new RequestType { Name = "Masraf", RequiresApproval = true }, new RequestType { Name = "Donanım", RequiresApproval = true }, new RequestType { Name = "Genel", RequiresApproval = false });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Demo geçici parolasını kaynak kod yerine ortam değişkeninden alır.</summary>
    private static string GetInitialPassword()
    {
        var password = Environment.GetEnvironmentVariable(InitialPasswordVariable);
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new InvalidOperationException($"{InitialPasswordVariable} en az 8 karakter olacak şekilde tanımlanmalıdır.");
        return password;
    }

    private static async Task EnsureMustChangePasswordColumnAsync(AppDbContext db, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        var closeAfter = connection.State != ConnectionState.Open;
        if (closeAfter) await connection.OpenAsync(ct);
        try
        {
            await using var check = connection.CreateCommand();
            check.CommandText = "PRAGMA table_info(\"Users\")";
            var exists = false;
            await using (var reader = await check.ExecuteReaderAsync(ct))
                while (await reader.ReadAsync(ct))
                    if (string.Equals(reader.GetString(1), "MustChangePassword", StringComparison.OrdinalIgnoreCase)) { exists = true; break; }
            if (!exists)
            {
                await using var alter = connection.CreateCommand();
                alter.CommandText = "ALTER TABLE \"Users\" ADD COLUMN \"MustChangePassword\" INTEGER NOT NULL DEFAULT 0";
                await alter.ExecuteNonQueryAsync(ct);
            }
        }
        finally
        {
            if (closeAfter) await connection.CloseAsync();
        }
    }
}
