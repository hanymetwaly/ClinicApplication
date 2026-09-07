using ClinicApp.Application.Interfaces;

namespace ClinicApp.Api.Tests;

public class TestFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public TestFileStorageService()
    {
        _basePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var fullPath = Path.Combine(_basePath, storedFileName);
        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);
        return storedFileName;
    }

    public Stream? OpenRead(string storedFileName)
    {
        var fullPath = Path.Combine(_basePath, storedFileName);
        return File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
    }
}
