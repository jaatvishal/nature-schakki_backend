using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Core.Exceptions;
using Core.Interfaces;
using Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class AzureBlobFileStorage : IFileStorageService
{
    private readonly BlobContainerClient _container;
    private readonly ILogger<AzureBlobFileStorage> _logger;

    public AzureBlobFileStorage(
        IConfiguration configuration,
        IOptions<FileStorageOptions> options,
        ILogger<AzureBlobFileStorage> logger)
    {
        _logger = logger;
        var connectionString = configuration.GetConnectionString("BlobStorage");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:BlobStorage must be configured.");

        var service = new BlobServiceClient(connectionString);
        _container = service.GetBlobContainerClient(options.Value.ContainerName);
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType)
    {
        try
        {
            await _container.CreateIfNotExistsAsync(PublicAccessType.Blob);
            var safeName = Path.GetFileName(fileName);
            var blobName = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}_{safeName}";
            var blob = _container.GetBlobClient(blobName);
            await blob.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });
            return blob.Uri.AbsoluteUri;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Blob product image upload failed.");
            throw new ServiceUnavailableException("File storage is temporarily unavailable.");
        }
    }

    public async Task DeleteFileAsync(string filePath)
    {
        if (!Uri.TryCreate(filePath, UriKind.Absolute, out var uri)) return;
        var prefix = $"/{_container.Name}/";
        var index = uri.AbsolutePath.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return;
        var blobName = Uri.UnescapeDataString(uri.AbsolutePath[(index + prefix.Length)..]);
        await _container.DeleteBlobIfExistsAsync(blobName);
    }
}
