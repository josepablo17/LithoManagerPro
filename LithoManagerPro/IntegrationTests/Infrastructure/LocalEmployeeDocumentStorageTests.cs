using System.Security.Cryptography;
using System.Text;
using LithoManager.Application.Abstractions.Storage;
using LithoManager.Infrastructure.Storage.Documents;
using Microsoft.Extensions.Options;
using Xunit;

namespace LithoManager.IntegrationTests.Infrastructure;

public sealed class LocalEmployeeDocumentStorageTests
{
    [Fact]
    public async Task SaveOpenAndDeleteAsync_WhenDocumentIsValid_PersistsAndRemovesFile()
    {
        // Arrange
        string rootPath =
            Path.Combine(
                Path.GetTempPath(),
                "LithoManager",
                "DocumentStorageTests",
                Guid.NewGuid().ToString("N"));

        LocalEmployeeDocumentStorage storage =
            new(
                Options.Create(
                    new DocumentStorageOptions
                    {
                        RootPath = rootPath,
                        MaximumFileSizeBytes = 1024
                    }));

        byte[] content =
            Encoding.UTF8.GetBytes(
                "LithoManager document test content.");

        await using MemoryStream input =
            new(content);

        try
        {
            // Act
            EmployeeDocumentStorageResult result =
                await storage.SaveAsync(
                    input,
                    originalFileName: "contract.txt",
                    contentType: "text/plain",
                    CancellationToken.None);

            // Assert
            Assert.Equal(
                "LocalFileSystem",
                result.StorageProvider);
            Assert.Equal(
                content.Length,
                result.FileSizeBytes);
            Assert.Equal(
                SHA256.HashData(content),
                result.FileHash);

            Stream? output =
                await storage.OpenReadAsync(
                    result.StorageProvider,
                    result.StorageKey,
                    CancellationToken.None);

            Assert.NotNull(output);

            await using (output)
            {
                using MemoryStream copied =
                    new();

                await output.CopyToAsync(
                    copied,
                    CancellationToken.None);

                Assert.Equal(
                    content,
                    copied.ToArray());
            }

            await storage.DeleteAsync(
                result.StorageProvider,
                result.StorageKey,
                CancellationToken.None);

            Stream? deleted =
                await storage.OpenReadAsync(
                    result.StorageProvider,
                    result.StorageKey,
                    CancellationToken.None);

            Assert.Null(deleted);
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(
                    rootPath,
                    recursive: true);
            }
        }
    }
}
