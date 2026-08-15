using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using LithoManager.Api.Contracts.Authentication;
using LithoManager.Api.Contracts.Documents;
using LithoManager.Application.Features
    .HumanResources.Departments;
using LithoManager.Application.Features.Documents;
using LithoManager.IntegrationTests
    .Api.Infrastructure;
using LithoManager.IntegrationTests.Collections;
using LithoManager.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LithoManager.IntegrationTests.Api.Documents;

[Collection(
    AuthenticationDatabaseCollection.Name)]
public sealed class DocumentEndpointsTests
{
    private readonly AuthenticationDatabaseFixture
        _databaseFixture;

    private readonly HttpClient _client;

    public DocumentEndpointsTests(
        AuthenticationDatabaseFixture databaseFixture,
        LithoManagerWebApplicationFactory applicationFactory)
    {
        ArgumentNullException.ThrowIfNull(
            databaseFixture);

        ArgumentNullException.ThrowIfNull(
            applicationFactory);

        _databaseFixture =
            databaseFixture;

        _client =
            applicationFactory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress =
                        new Uri("https://localhost"),

                    AllowAutoRedirect = false
                });
    }

    [Fact]
    public async Task CreateAndDownloadDocument_WhenRequestIsValid_ReturnsDocumentAndFileContent()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        string identificationNumber =
            CreateIdentificationNumber();

        Guid correlationId =
            Guid.NewGuid();

        await CleanupAsync(
            departmentCode,
            identificationNumber);

        try
        {
            int employeeId =
                await CreateEmployeeAsync(
                    departmentCode,
                    identificationNumber);

            DocumentTypeData documentType =
                await GetEmploymentContractTypeAsync();

            string accessToken =
                await LoginAndGetAccessTokenAsync();

            byte[] fileContent =
                Encoding.UTF8.GetBytes(
                    "LithoManager API document test.");

            using MultipartFormDataContent multipart =
                CreateCreateDocumentMultipart(
                    employeeId,
                    documentType.DocumentTypeId,
                    fileContent);

            using HttpRequestMessage request =
                new(
                    HttpMethod.Post,
                    "/api/documents");

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    scheme: "Bearer",
                    parameter: accessToken);

            request.Headers.Add(
                "X-Correlation-ID",
                correlationId.ToString());

            request.Content = multipart;

            // Act
            HttpResponseMessage response =
                await _client.SendAsync(request);

            // Assert
            Assert.Equal(
                HttpStatusCode.Created,
                response.StatusCode);

            EmployeeDocumentResponse? document =
                await response.Content
                    .ReadFromJsonAsync<
                        EmployeeDocumentResponse>();

            Assert.NotNull(document);
            Assert.True(
                document.EmployeeDocumentId > 0);
            Assert.Equal(
                employeeId,
                document.EmployeeId);
            Assert.Equal(
                "Employment contract",
                document.Title);
            Assert.Equal(
                "contract.txt",
                document.OriginalFileName);
            Assert.Equal(
                fileContent.Length,
                document.FileSizeBytes);
            Assert.Equal(
                8,
                Convert.FromBase64String(
                    document.RowVersion).Length);

            using HttpRequestMessage downloadRequest =
                CreateAuthorizedRequest(
                    HttpMethod.Get,
                    "/api/documents/" +
                    document.EmployeeDocumentId +
                    "/download",
                    accessToken,
                    Guid.NewGuid());

            HttpResponseMessage downloadResponse =
                await _client.SendAsync(downloadRequest);

            Assert.Equal(
                HttpStatusCode.OK,
                downloadResponse.StatusCode);

            byte[] downloaded =
                await downloadResponse.Content
                    .ReadAsByteArrayAsync();

            Assert.Equal(
                fileContent,
                downloaded);

            AuditLogTestData? audit =
                await _databaseFixture
                    .GetAuditLogByCorrelationIdAsync(
                        correlationId);

            Assert.NotNull(audit);
            Assert.Equal(
                "Documents",
                audit.ModuleName);
            Assert.Equal(
                "EmployeeDocumentCreated",
                audit.ActionName);
            Assert.Equal(
                document.EmployeeDocumentId.ToString(),
                audit.EntityId);
        }
        finally
        {
            await CleanupAsync(
                departmentCode,
                identificationNumber);
        }
    }

    private static MultipartFormDataContent
        CreateCreateDocumentMultipart(
            int employeeId,
            int documentTypeId,
            byte[] fileContent)
    {
        MultipartFormDataContent multipart =
            new();

        multipart.Add(
            new StringContent(
                employeeId.ToString()),
            "EmployeeId");

        multipart.Add(
            new StringContent(
                documentTypeId.ToString()),
            "DocumentTypeId");

        multipart.Add(
            new StringContent(
                "Employment contract"),
            "Title");

        multipart.Add(
            new StringContent(
                "Created by API integration tests."),
            "Description");

        multipart.Add(
            new StringContent("true"),
            "IsVisibleToEmployee");

        ByteArrayContent fileContentPart =
            new(fileContent);

        fileContentPart.Headers.ContentType =
            new MediaTypeHeaderValue("text/plain");

        multipart.Add(
            fileContentPart,
            "File",
            "contract.txt");

        return multipart;
    }

    private async Task<int> CreateEmployeeAsync(
        string departmentCode,
        string identificationNumber)
    {
        DepartmentData department =
            await _databaseFixture.DepartmentRepository
                .CreateDepartmentAsync(
                    departmentCode:
                        departmentCode,
                    name:
                        "Documents API Tests",
                    description:
                        "Created by API integration tests.",
                    actorUserId:
                        _databaseFixture
                            .SuperAdministratorUserId,
                    requestContext:
                        AuthenticationDatabaseFixture
                            .CreateRequestContext(
                                "/integration-tests/" +
                                "documents/api/" +
                                "create-department"),
                    cancellationToken:
                        CancellationToken.None);

        await _databaseFixture
            .CreateActiveEmployeeForDepartmentAsync(
                department.DepartmentId,
                identificationNumber);

        return await _databaseFixture
            .GetEmployeeIdByIdentificationNumberAsync(
                identificationNumber);
    }

    private async Task<DocumentTypeData>
        GetEmploymentContractTypeAsync()
    {
        IReadOnlyList<DocumentTypeData> documentTypes =
            await _databaseFixture.DocumentRepository
                .GetDocumentTypesAsync(
                    actorUserId:
                        _databaseFixture
                            .SuperAdministratorUserId,
                    isActive:
                        true,
                    cancellationToken:
                        CancellationToken.None);

        return Assert.Single(
            documentTypes,
            documentType =>
                documentType.DocumentTypeCode
                    == "EmploymentContract");
    }

    private async Task<string> LoginAndGetAccessTokenAsync()
    {
        LoginRequest loginRequest =
            new()
            {
                EmailAddress =
                    AuthenticationDatabaseFixture
                        .TestEmailAddress,

                Password =
                    AuthenticationDatabaseFixture
                        .TestPassword
            };

        HttpResponseMessage loginResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                loginRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        LoginResponse? loginBody =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginBody);
        Assert.False(
            string.IsNullOrWhiteSpace(
                loginBody.AccessToken));

        return loginBody.AccessToken;
    }

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string requestUri,
        string accessToken,
        Guid correlationId)
    {
        HttpRequestMessage request =
            new(
                method,
                requestUri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                scheme: "Bearer",
                parameter: accessToken);

        request.Headers.Add(
            "X-Correlation-ID",
            correlationId.ToString());

        return request;
    }

    private async Task CleanupAsync(
        string departmentCode,
        string identificationNumber)
    {
        await _databaseFixture.RemoveDocumentTestDataAsync(
            identificationNumber);

        await _databaseFixture.RemoveDepartmentTestDataAsync(
            departmentCode,
            identificationNumber);
    }

    private static string CreateDepartmentCode()
    {
        return "DA" + Guid.NewGuid()
            .ToString("N")[..12]
            .ToUpperInvariant();
    }

    private static string CreateIdentificationNumber()
    {
        return "DAPI-" + Guid.NewGuid()
            .ToString("N")[..12]
            .ToUpperInvariant();
    }
}
