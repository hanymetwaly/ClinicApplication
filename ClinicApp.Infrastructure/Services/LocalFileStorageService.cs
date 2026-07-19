using ClinicApp.Application.Interfaces;

namespace ClinicApp.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadsPath;

    public LocalFileStorageService(string uploadsPath)
    {
        _uploadsPath = uploadsPath;
        Directory.CreateDirectory(_uploadsPath);
    }

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var fullPath = Path.Combine(_uploadsPath, storedFileName);
        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);
        return storedFileName;
    }

    public Stream? OpenRead(string storedFileName)
    {
        var fullPath = Path.Combine(_uploadsPath, storedFileName);
        return File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
    }
}
