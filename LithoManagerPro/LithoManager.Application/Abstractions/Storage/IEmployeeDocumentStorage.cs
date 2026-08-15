namespace LithoManager.Application.Abstractions.Storage;

public interface IEmployeeDocumentStorage
{
    Task<EmployeeDocumentStorageResult> SaveAsync(
        Stream content,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(
        string storageProvider,
        string storageKey,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string storageProvider,
        string storageKey,
        CancellationToken cancellationToken);
}
