namespace LithoManager.Infrastructure.Storage.Documents;

public sealed class DocumentStorageOptions
{
    public const string SectionName =
        "Documents:Storage";

    public string ProviderName { get; init; } =
        "LocalFileSystem";

    public string? RootPath { get; init; }

    public long MaximumFileSizeBytes { get; init; } =
        25 * 1024 * 1024;
}
