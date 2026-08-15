using Core.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace Infrastructure.Services;

public class LocalFileStorage(IWebHostEnvironment env) : IFileStorageService
{
    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var uploadsPath = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(uploadsPath);
        var uniqueName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(uploadsPath, uniqueName);
        await using var stream = File.Create(filePath);
        await fileStream.CopyToAsync(stream);
        return $"/uploads/{uniqueName}";
    }

    public Task DeleteFileAsync(string filePath)
    {
        var fullPath = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), filePath.TrimStart('/'));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
