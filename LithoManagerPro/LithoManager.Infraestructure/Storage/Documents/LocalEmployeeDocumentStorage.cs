using System.Buffers;
using System.Security.Cryptography;
using LithoManager.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;

namespace LithoManager.Infrastructure.Storage.Documents;

public sealed class LocalEmployeeDocumentStorage
    : IEmployeeDocumentStorage
{
    private const int BufferSize = 81920;

    private readonly DocumentStorageOptions _options;
    private readonly string _rootPath;

    public LocalEmployeeDocumentStorage(
        IOptions<DocumentStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;

        _rootPath =
            string.IsNullOrWhiteSpace(_options.RootPath)
                ? Path.Combine(
                    AppContext.BaseDirectory,
                    "App_Data",
                    "EmployeeDocuments")
                : _options.RootPath;
    }

    public async Task<EmployeeDocumentStorageResult> SaveAsync(
        Stream content,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(
            originalFileName);

        if (!content.CanRead)
        {
            throw new ArgumentException(
                "The document stream must be readable.",
                nameof(content));
        }

        string storageKey =
            CreateStorageKey();

        string fullPath =
            ResolveStorageKey(storageKey);

        Directory.CreateDirectory(
            Path.GetDirectoryName(fullPath)!);

        byte[] buffer =
            ArrayPool<byte>.Shared.Rent(BufferSize);

        long totalBytes = 0;

        try
        {
            using IncrementalHash hash =
                IncrementalHash.CreateHash(
                    HashAlgorithmName.SHA256);

            await using FileStream output =
                new(
                    fullPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    BufferSize,
                    FileOptions.Asynchronous);

            while (true)
            {
                int bytesRead =
                    await content.ReadAsync(
                        buffer.AsMemory(0, BufferSize),
                        cancellationToken);

                if (bytesRead == 0)
                {
                    break;
                }

                totalBytes += bytesRead;

                if (totalBytes
                    > _options.MaximumFileSizeBytes)
                {
                    throw new InvalidOperationException(
                        "The document exceeds the maximum allowed size.");
                }

                hash.AppendData(buffer, 0, bytesRead);

                await output.WriteAsync(
                    buffer.AsMemory(0, bytesRead),
                    cancellationToken);
            }

            if (totalBytes == 0)
            {
                throw new InvalidOperationException(
                    "The document cannot be empty.");
            }

            return new EmployeeDocumentStorageResult(
                StorageProvider:
                    _options.ProviderName,
                StorageKey:
                    storageKey,
                FileSizeBytes:
                    totalBytes,
                FileHash:
                    hash.GetHashAndReset());
        }
        catch
        {
            DeleteFileIfExists(fullPath);
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public Task<Stream?> OpenReadAsync(
        string storageProvider,
        string storageKey,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            storageProvider);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            storageKey);

        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(
                storageProvider,
                _options.ProviderName,
                StringComparison.Ordinal))
        {
            return Task.FromResult<Stream?>(null);
        }

        string fullPath =
            ResolveStorageKey(storageKey);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream =
            new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                BufferSize,
                FileOptions.Asynchronous);

        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(
        string storageProvider,
        string storageKey,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            storageProvider);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            storageKey);

        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(
                storageProvider,
                _options.ProviderName,
                StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        DeleteFileIfExists(
            ResolveStorageKey(storageKey));

        return Task.CompletedTask;
    }

    private static string CreateStorageKey()
    {
        DateTime now =
            DateTime.UtcNow;

        return string.Join(
            '/',
            now.ToString("yyyy"),
            now.ToString("MM"),
            now.ToString("dd"),
            Guid.NewGuid().ToString("N"));
    }

    private string ResolveStorageKey(string storageKey)
    {
        string normalizedStorageKey =
            storageKey.Replace(
                '/',
                Path.DirectorySeparatorChar);

        string fullPath =
            Path.GetFullPath(
                Path.Combine(
                    _rootPath,
                    normalizedStorageKey));

        string rootFullPath =
            Path.GetFullPath(_rootPath);

        if (!fullPath.StartsWith(
                rootFullPath,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The document storage key is not valid.");
        }

        return fullPath;
    }

    private static void DeleteFileIfExists(
        string fullPath)
    {
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
