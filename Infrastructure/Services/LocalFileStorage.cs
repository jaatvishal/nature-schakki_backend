using Core.Interfaces;
using Infrastructure.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class LocalFileStorage(
    IWebHostEnvironment env,
    IOptions<FileStorageOptions> options) : IFileStorageService
{
    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var uploadsPath = GetUploadsPath(env, options.Value);
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
        var root = Path.GetFullPath(GetUploadsPath(env, options.Value));
        var relativePath = filePath.TrimStart('/', '\\');
        if (relativePath.StartsWith($"uploads{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            relativePath = relativePath["uploads/".Length..];
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        if (fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
            File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public static string GetUploadsPath(
        IWebHostEnvironment environment,
        FileStorageOptions storageOptions)
    {
        if (!string.IsNullOrWhiteSpace(storageOptions.LocalPath))
            return Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(storageOptions.LocalPath));

        var appServiceHome = Environment.GetEnvironmentVariable("HOME");
        if (environment.IsProduction() && !string.IsNullOrWhiteSpace(appServiceHome))
            return Path.Combine(appServiceHome, "data", "NaturesChakki", "uploads");

        var webRoot = environment.WebRootPath
            ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        return Path.Combine(webRoot, "uploads");
    }
}
