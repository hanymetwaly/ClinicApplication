namespace ClinicApp.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
    Stream? OpenRead(string storedFileName);
}
