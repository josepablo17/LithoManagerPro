namespace LithoManager.Application.Abstractions.Storage;

public sealed record EmployeeDocumentStorageResult(
    string StorageProvider,
    string StorageKey,
    long FileSizeBytes,
    byte[] FileHash);
