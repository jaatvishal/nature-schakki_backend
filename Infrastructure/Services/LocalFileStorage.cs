using Core.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace Infrastructure.Services;

public class LocalFileStorage(IWebHostEnvironment env) : IFileStorageService
{
    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var uploadsPath = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(uploadsPath);
        var safeName = Path.GetFileName(fileName);
        var uniqueName = $"{Guid.NewGuid():N}_{safeName}";
        var filePath = Path.Combine(uploadsPath, uniqueName);
        await using var stream = File.Create(filePath);
        await fileStream.CopyToAsync(stream);
        return $"/uploads/{uniqueName}";
    }

    public Task DeleteFileAsync(string filePath)
    {
        var root = Path.GetFullPath(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"));
        var fullPath = Path.GetFullPath(Path.Combine(root, filePath.TrimStart('/', '\\')));
        if (fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) && File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
