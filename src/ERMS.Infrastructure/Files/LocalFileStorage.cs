using ERMS.Application.Common;
using ERMS.Application.DTOs;
using ERMS.Application.Interfaces;
namespace ERMS.Infrastructure.Files;
public sealed class LocalFileStorage(string rootPath) : IFileStorage
{
    public async Task<string> SaveAsync(FilePayload file, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(rootPath); var stored = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}"; var path = SafePath(stored);
        await using var target = File.Create(path); await file.Content.CopyToAsync(target, cancellationToken); return stored;
    }
    public Task<StoredFile> OpenAsync(string storedFileName, string originalFileName, CancellationToken cancellationToken)
    {
        var path = SafePath(storedFileName); if (!File.Exists(path)) throw new NotFoundException("Dosya diskte bulunamadı."); return Task.FromResult(new StoredFile(originalFileName, File.OpenRead(path)));
    }
    private string SafePath(string name) { var path = Path.GetFullPath(Path.Combine(rootPath, Path.GetFileName(name))); var root = Path.GetFullPath(rootPath) + Path.DirectorySeparatorChar; if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new ForbiddenException(); return path; }
}
