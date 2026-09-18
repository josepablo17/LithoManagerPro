using System.Data;
using Dapper;
using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features.Documents;
using LithoManager.Infrastructure.Persistence.Dapper;
using Microsoft.Data.SqlClient;

namespace LithoManager.Infrastructure.Persistence
    .Repositories.Documents;

public sealed class DocumentRepository
    : IDocumentRepository
{
    private const string GetDocumentTypesProcedure =
        "Documents.GetDocumentTypes";

    private const string EnsureEmployeeRecordProcedure =
        "Documents.EnsureEmployeeRecord";

    private const string GetEmployeeDocumentsProcedure =
        "Documents.GetEmployeeDocuments";

    private const string GetEmployeeDocumentByIdProcedure =
        "Documents.GetEmployeeDocumentById";

    private const string GetEmployeeDocumentDownloadContextProcedure =
        "Documents.GetEmployeeDocumentDownloadContext";

    private const string CreateEmployeeDocumentProcedure =
        "Documents.CreateEmployeeDocument";

    private const string UpdateEmployeeDocumentProcedure =
        "Documents.UpdateEmployeeDocument";

    private const string SetEmployeeDocumentStatusProcedure =
        "Documents.SetEmployeeDocumentStatus";

    private readonly ISqlConnectionFactory _connectionFactory;

    public DocumentRepository(
        ISqlConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<DocumentTypeData>>
        GetDocumentTypesAsync(
            int actorUserId,
            bool? isActive,
            CancellationToken cancellationToken)
    {
        ValidateActorUserId(actorUserId);

        var parameters = new
        {
            ActorUserId = actorUserId,
            IsActive = isActive
        };

        CommandDefinition command = CreateCommand(
            GetDocumentTypesProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        IReadOnlyList<DocumentTypeData> documentTypes =
            await QueryAsync<DocumentTypeData>(
                connection,
                command);

        return documentTypes
            .Select(NormalizeDates)
            .ToList();
    }

    public async Task<EmployeeRecordData> EnsureEmployeeRecordAsync(
        int employeeId,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ValidateEmployeeId(employeeId);
        ValidateActorUserId(actorUserId);
        ValidateRequestContext(requestContext);

        DynamicParameters parameters = new();

        parameters.Add(
            "EmployeeId",
            employeeId,
            DbType.Int32,
            ParameterDirection.Input);

        AddActorAndRequestContextParameters(
            parameters,
            actorUserId,
            requestContext);

        CommandDefinition command = CreateCommand(
            EnsureEmployeeRecordProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        EmployeeRecordData employeeRecord =
            await QuerySingleAsync<EmployeeRecordData>(
                connection,
                command);

        return NormalizeDates(employeeRecord);
    }

    public async Task<IReadOnlyList<EmployeeDocumentData>>
        GetEmployeeDocumentsAsync(
            int actorUserId,
            int? employeeId,
            int? documentTypeId,
            bool? isActive,
            bool? isVisibleToEmployee,
            DateTime? createdFromUtc,
            DateTime? createdToUtc,
            string? searchTerm,
            CancellationToken cancellationToken)
    {
        ValidateActorUserId(actorUserId);
        ValidateOptionalEmployeeId(employeeId);
        ValidateOptionalDocumentTypeId(documentTypeId);
        ValidateDateRange(createdFromUtc, createdToUtc);

        var parameters = new
        {
            ActorUserId = actorUserId,
            EmployeeId = employeeId,
            DocumentTypeId = documentTypeId,
            IsActive = isActive,
            IsVisibleToEmployee = isVisibleToEmployee,
            CreatedFromUtc = createdFromUtc,
            CreatedToUtc = createdToUtc,
            SearchTerm = NormalizeOptionalString(searchTerm)
        };

        CommandDefinition command = CreateCommand(
            GetEmployeeDocumentsProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        IReadOnlyList<EmployeeDocumentData> documents =
            await QueryAsync<EmployeeDocumentData>(
                connection,
                command);

        return documents
            .Select(NormalizeDates)
            .ToList();
    }

    public async Task<EmployeeDocumentData?>
        GetEmployeeDocumentByIdAsync(
            int employeeDocumentId,
            int actorUserId,
            CancellationToken cancellationToken)
    {
        ValidateEmployeeDocumentId(employeeDocumentId);
        ValidateActorUserId(actorUserId);

        var parameters = new
        {
            EmployeeDocumentId = employeeDocumentId,
            ActorUserId = actorUserId
        };

        CommandDefinition command = CreateCommand(
            GetEmployeeDocumentByIdProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        EmployeeDocumentData? result =
            await QuerySingleOrDefaultAsync<EmployeeDocumentData>(
                connection,
                command);

        return result is null
            ? null
            : NormalizeDates(result);
    }

    public async Task<EmployeeDocumentDownloadContextData?>
        GetEmployeeDocumentDownloadContextAsync(
            int employeeDocumentId,
            int actorUserId,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        ValidateEmployeeDocumentId(employeeDocumentId);
        ValidateActorUserId(actorUserId);
        ValidateRequestContext(requestContext);

        DynamicParameters parameters = new();

        parameters.Add(
            "EmployeeDocumentId",
            employeeDocumentId,
            DbType.Int32,
            ParameterDirection.Input);

        AddActorAndRequestContextParameters(
            parameters,
            actorUserId,
            requestContext);

        CommandDefinition command = CreateCommand(
            GetEmployeeDocumentDownloadContextProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QuerySingleOrDefaultAsync<
            EmployeeDocumentDownloadContextData>(
                connection,
                command);
    }

    public async Task<EmployeeDocumentData>
        CreateEmployeeDocumentAsync(
            int employeeId,
            int documentTypeId,
            string title,
            string? description,
            string originalFileName,
            string storageProvider,
            string storageKey,
            string contentType,
            long fileSizeBytes,
            byte[] fileHash,
            DateTime? issuedDate,
            DateTime? expirationDate,
            bool? isVisibleToEmployee,
            int actorUserId,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        ValidateEmployeeId(employeeId);
        ValidateDocumentTypeId(documentTypeId);
        ValidateRequiredString(title, nameof(title));
        ValidateRequiredString(
            originalFileName,
            nameof(originalFileName));
        ValidateRequiredString(
            storageProvider,
            nameof(storageProvider));
        ValidateRequiredString(storageKey, nameof(storageKey));
        ValidateRequiredString(contentType, nameof(contentType));
        ValidateFileSize(fileSizeBytes);
        ValidateFileHash(fileHash);
        ValidateDocumentDates(issuedDate, expirationDate);
        ValidateActorUserId(actorUserId);
        ValidateRequestContext(requestContext);

        DynamicParameters parameters = new();

        parameters.Add(
            "EmployeeId",
            employeeId,
            DbType.Int32,
            ParameterDirection.Input);

        parameters.Add(
            "DocumentTypeId",
            documentTypeId,
            DbType.Int32,
            ParameterDirection.Input);

        parameters.Add(
            "Title",
            title.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 4000);

        parameters.Add(
            "Description",
            NormalizeOptionalString(description),
            DbType.String,
            ParameterDirection.Input,
            size: 4000);

        parameters.Add(
            "OriginalFileName",
            originalFileName.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 4000);

        parameters.Add(
            "StorageProvider",
            storageProvider.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 4000);

        parameters.Add(
            "StorageKey",
            storageKey.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 4000);

        parameters.Add(
            "ContentType",
            contentType.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 4000);

        parameters.Add(
            "FileSizeBytes",
            fileSizeBytes,
            DbType.Int64,
            ParameterDirection.Input);

        parameters.Add(
            "FileHash",
            fileHash,
            DbType.Binary,
            ParameterDirection.Input,
            size: 32);

        parameters.Add(
            "IssuedDate",
            issuedDate?.Date,
            DbType.Date,
            ParameterDirection.Input);

        parameters.Add(
            "ExpirationDate",
            expirationDate?.Date,
            DbType.Date,
            ParameterDirection.Input);

        parameters.Add(
            "IsVisibleToEmployee",
            isVisibleToEmployee,
            DbType.Boolean,
            ParameterDirection.Input);

        AddActorAndRequestContextParameters(
            parameters,
            actorUserId,
            requestContext);

        CommandDefinition command = CreateCommand(
            CreateEmployeeDocumentProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        EmployeeDocumentData result =
            await QuerySingleAsync<EmployeeDocumentData>(
                connection,
                command);

        return NormalizeDates(result);
    }

    public async Task<EmployeeDocumentData>
        UpdateEmployeeDocumentAsync(
            int employeeDocumentId,
            int documentTypeId,
            string title,
            string? description,
            DateTime? issuedDate,
            DateTime? expirationDate,
            bool isVisibleToEmployee,
            byte[] expectedRowVersion,
            int actorUserId,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        ValidateEmployeeDocumentId(employeeDocumentId);
        ValidateDocumentTypeId(documentTypeId);
        ValidateRequiredString(title, nameof(title));
        ValidateDocumentDates(issuedDate, expirationDate);
        ValidateRowVersion(expectedRowVersion);
        ValidateActorUserId(actorUserId);
        ValidateRequestContext(requestContext);

        DynamicParameters parameters = new();

        parameters.Add(
            "EmployeeDocumentId",
            employeeDocumentId,
            DbType.Int32,
            ParameterDirection.Input);

        parameters.Add(
            "DocumentTypeId",
            documentTypeId,
            DbType.Int32,
            ParameterDirection.Input);

        parameters.Add(
            "Title",
            title.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 4000);

        parameters.Add(
            "Description",
            NormalizeOptionalString(description),
            DbType.String,
            ParameterDirection.Input,
            size: 4000);

        parameters.Add(
            "IssuedDate",
            issuedDate?.Date,
            DbType.Date,
            ParameterDirection.Input);

        parameters.Add(
            "ExpirationDate",
            expirationDate?.Date,
            DbType.Date,
            ParameterDirection.Input);

        parameters.Add(
            "IsVisibleToEmployee",
            isVisibleToEmployee,
            DbType.Boolean,
            ParameterDirection.Input);

        parameters.Add(
            "ExpectedRowVersion",
            expectedRowVersion,
            DbType.Binary,
            ParameterDirection.Input,
            size: 8);

        AddActorAndRequestContextParameters(
            parameters,
            actorUserId,
            requestContext);

        CommandDefinition command = CreateCommand(
            UpdateEmployeeDocumentProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        EmployeeDocumentData result =
            await QuerySingleAsync<EmployeeDocumentData>(
                connection,
                command);

        return NormalizeDates(result);
    }

    public async Task<EmployeeDocumentData>
        SetEmployeeDocumentStatusAsync(
            int employeeDocumentId,
            bool isActive,
            byte[] expectedRowVersion,
            int actorUserId,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        ValidateEmployeeDocumentId(employeeDocumentId);
        ValidateRowVersion(expectedRowVersion);
        ValidateActorUserId(actorUserId);
        ValidateRequestContext(requestContext);

        DynamicParameters parameters = new();

        parameters.Add(
            "EmployeeDocumentId",
            employeeDocumentId,
            DbType.Int32,
            ParameterDirection.Input);

        parameters.Add(
            "IsActive",
            isActive,
            DbType.Boolean,
            ParameterDirection.Input);

        parameters.Add(
            "ExpectedRowVersion",
            expectedRowVersion,
            DbType.Binary,
            ParameterDirection.Input,
            size: 8);

        AddActorAndRequestContextParameters(
            parameters,
            actorUserId,
            requestContext);

        CommandDefinition command = CreateCommand(
            SetEmployeeDocumentStatusProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        EmployeeDocumentData result =
            await QuerySingleAsync<EmployeeDocumentData>(
                connection,
                command);

        return NormalizeDates(result);
    }

    private static void AddActorAndRequestContextParameters(
        DynamicParameters parameters,
        int actorUserId,
        AuthenticationRequestContext requestContext)
    {
        parameters.Add(
            "ActorUserId",
            actorUserId,
            DbType.Int32,
            ParameterDirection.Input);

        parameters.Add(
            "CorrelationId",
            requestContext.CorrelationId,
            DbType.Guid,
            ParameterDirection.Input);

        parameters.Add(
            "ClientIpAddress",
            requestContext.ClientIpAddress,
            DbType.String,
            ParameterDirection.Input,
            size: 45);

        parameters.Add(
            "UserAgent",
            requestContext.UserAgent,
            DbType.String,
            ParameterDirection.Input,
            size: 512);

        parameters.Add(
            "RequestPath",
            requestContext.RequestPath,
            DbType.String,
            ParameterDirection.Input,
            size: 500);
    }

    private static CommandDefinition CreateCommand(
        string procedureName,
        object? parameters,
        CancellationToken cancellationToken)
    {
        return new CommandDefinition(
            commandText:
                procedureName,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);
    }

    private static string? NormalizeOptionalString(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static void ValidateEmployeeId(int employeeId)
    {
        if (employeeId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(employeeId),
                "EmployeeId must be greater than zero.");
        }
    }

    private static void ValidateOptionalEmployeeId(
        int? employeeId)
    {
        if (employeeId is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(employeeId),
                "EmployeeId must be greater than zero.");
        }
    }

    private static void ValidateDocumentTypeId(
        int documentTypeId)
    {
        if (documentTypeId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(documentTypeId),
                "DocumentTypeId must be greater than zero.");
        }
    }

    private static void ValidateOptionalDocumentTypeId(
        int? documentTypeId)
    {
        if (documentTypeId is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(documentTypeId),
                "DocumentTypeId must be greater than zero.");
        }
    }

    private static void ValidateEmployeeDocumentId(
        int employeeDocumentId)
    {
        if (employeeDocumentId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(employeeDocumentId),
                "EmployeeDocumentId must be greater than zero.");
        }
    }

    private static void ValidateActorUserId(int actorUserId)
    {
        if (actorUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actorUserId),
                "ActorUserId must be greater than zero.");
        }
    }

    private static void ValidateDateRange(
        DateTime? from,
        DateTime? to)
    {
        if (from.HasValue
            && to.HasValue
            && to.Value < from.Value)
        {
            throw new ArgumentException(
                "CreatedToUtc cannot be earlier than CreatedFromUtc.",
                nameof(to));
        }
    }

    private static void ValidateDocumentDates(
        DateTime? issuedDate,
        DateTime? expirationDate)
    {
        if (issuedDate.HasValue
            && expirationDate.HasValue
            && expirationDate.Value.Date
                < issuedDate.Value.Date)
        {
            throw new ArgumentException(
                "ExpirationDate cannot be earlier than IssuedDate.",
                nameof(expirationDate));
        }
    }

    private static void ValidateRequiredString(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Value is required.",
                parameterName);
        }
    }

    private static void ValidateFileSize(long fileSizeBytes)
    {
        if (fileSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fileSizeBytes),
                "FileSizeBytes must be greater than zero.");
        }
    }

    private static void ValidateFileHash(byte[] fileHash)
    {
        ArgumentNullException.ThrowIfNull(fileHash);

        if (fileHash.Length != 32)
        {
            throw new ArgumentException(
                "FileHash must contain exactly 32 bytes.",
                nameof(fileHash));
        }
    }

    private static void ValidateRequestContext(
        AuthenticationRequestContext requestContext)
    {
        ArgumentNullException.ThrowIfNull(requestContext);

        if (requestContext.CorrelationId == Guid.Empty)
        {
            throw new ArgumentException(
                "CorrelationId is required.",
                nameof(requestContext));
        }
    }

    private static void ValidateRowVersion(
        byte[] expectedRowVersion)
    {
        ArgumentNullException.ThrowIfNull(
            expectedRowVersion);

        if (expectedRowVersion.Length != 8)
        {
            throw new ArgumentException(
                "ExpectedRowVersion must contain exactly 8 bytes.",
                nameof(expectedRowVersion));
        }
    }

    private static DocumentTypeData NormalizeDates(
        DocumentTypeData documentType)
    {
        return new DocumentTypeData
        {
            DocumentTypeId = documentType.DocumentTypeId,
            DocumentTypeCode = documentType.DocumentTypeCode,
            Name = documentType.Name,
            Description = documentType.Description,
            DefaultIsVisibleToEmployee =
                documentType.DefaultIsVisibleToEmployee,
            IsActive = documentType.IsActive,
            CreatedAtUtc = SpecifyUtc(
                documentType.CreatedAtUtc),
            CreatedByUserId = documentType.CreatedByUserId,
            UpdatedAtUtc = SpecifyNullableUtc(
                documentType.UpdatedAtUtc),
            UpdatedByUserId = documentType.UpdatedByUserId,
            RowVersion = documentType.RowVersion
        };
    }

    private static EmployeeRecordData NormalizeDates(
        EmployeeRecordData employeeRecord)
    {
        return new EmployeeRecordData
        {
            EmployeeRecordId =
                employeeRecord.EmployeeRecordId,
            EmployeeId = employeeRecord.EmployeeId,
            IdentificationNumber =
                employeeRecord.IdentificationNumber,
            FirstName = employeeRecord.FirstName,
            LastName = employeeRecord.LastName,
            DepartmentId = employeeRecord.DepartmentId,
            DepartmentCode = employeeRecord.DepartmentCode,
            DepartmentName = employeeRecord.DepartmentName,
            CreatedAtUtc = SpecifyUtc(
                employeeRecord.CreatedAtUtc),
            CreatedByUserId = employeeRecord.CreatedByUserId,
            UpdatedAtUtc = SpecifyNullableUtc(
                employeeRecord.UpdatedAtUtc),
            UpdatedByUserId = employeeRecord.UpdatedByUserId,
            RowVersion = employeeRecord.RowVersion
        };
    }

    private static EmployeeDocumentData NormalizeDates(
        EmployeeDocumentData document)
    {
        return new EmployeeDocumentData
        {
            EmployeeDocumentId =
                document.EmployeeDocumentId,
            EmployeeRecordId = document.EmployeeRecordId,
            EmployeeId = document.EmployeeId,
            IdentificationNumber =
                document.IdentificationNumber,
            FirstName = document.FirstName,
            LastName = document.LastName,
            DepartmentId = document.DepartmentId,
            DepartmentCode = document.DepartmentCode,
            DepartmentName = document.DepartmentName,
            DocumentTypeId = document.DocumentTypeId,
            DocumentTypeCode = document.DocumentTypeCode,
            DocumentTypeName = document.DocumentTypeName,
            Title = document.Title,
            Description = document.Description,
            OriginalFileName = document.OriginalFileName,
            ContentType = document.ContentType,
            FileSizeBytes = document.FileSizeBytes,
            FileHashAlgorithm = document.FileHashAlgorithm,
            IssuedDate = document.IssuedDate?.Date,
            ExpirationDate = document.ExpirationDate?.Date,
            IsVisibleToEmployee =
                document.IsVisibleToEmployee,
            IsActive = document.IsActive,
            DeactivatedAtUtc = SpecifyNullableUtc(
                document.DeactivatedAtUtc),
            DeactivatedByUserId =
                document.DeactivatedByUserId,
            CreatedAtUtc = SpecifyUtc(
                document.CreatedAtUtc),
            CreatedByUserId = document.CreatedByUserId,
            UpdatedAtUtc = SpecifyNullableUtc(
                document.UpdatedAtUtc),
            UpdatedByUserId = document.UpdatedByUserId,
            RowVersion = document.RowVersion
        };
    }

    private static DateTime SpecifyUtc(DateTime value)
    {
        return DateTime.SpecifyKind(
            value,
            DateTimeKind.Utc);
    }

    private static DateTime? SpecifyNullableUtc(
        DateTime? value)
    {
        return value is DateTime dateTime
            ? DateTime.SpecifyKind(
                dateTime,
                DateTimeKind.Utc)
            : null;
    }

    private static async Task<IReadOnlyList<T>> QueryAsync<T>(
        System.Data.Common.DbConnection connection,
        CommandDefinition command)
    {
        try
        {
            IEnumerable<T> result =
                await connection.QueryAsync<T>(command);

            return result.ToList();
        }
        catch (SqlException exception)
            when (TryMapSqlException(
                exception,
                out DocumentErrorCode errorCode))
        {
            throw new DocumentPersistenceException(
                errorCode,
                exception.Message,
                exception);
        }
    }

    private static async Task<T> QuerySingleAsync<T>(
        System.Data.Common.DbConnection connection,
        CommandDefinition command)
    {
        try
        {
            return await connection.QuerySingleAsync<T>(
                command);
        }
        catch (SqlException exception)
            when (TryMapSqlException(
                exception,
                out DocumentErrorCode errorCode))
        {
            throw new DocumentPersistenceException(
                errorCode,
                exception.Message,
                exception);
        }
    }

    private static async Task<T?> QuerySingleOrDefaultAsync<T>(
        System.Data.Common.DbConnection connection,
        CommandDefinition command)
    {
        try
        {
            return await connection.QuerySingleOrDefaultAsync<T>(
                command);
        }
        catch (SqlException exception)
            when (TryMapSqlException(
                exception,
                out DocumentErrorCode errorCode))
        {
            throw new DocumentPersistenceException(
                errorCode,
                exception.Message,
                exception);
        }
    }

    private static bool TryMapSqlException(
        SqlException exception,
        out DocumentErrorCode errorCode)
    {
        errorCode = exception.Number switch
        {
            2601 or 2627 =>
                DocumentErrorCode.DuplicateStorageKey,

            55001 or 55101 or 55102
                or 55201 or 55202 or 55203
                or 55204 or 55205
                or 55301 or 55302
                or 55401 or 55402
                or 55501 or 55502 or 55503
                or 55504 or 55505 or 55506
                or 55507 or 55508 or 55509
                or 55510 or 55511 or 55512
                or 55513 or 55514 or 55515
                or 55516 or 55517
                or 55601 or 55602 or 55603
                or 55604 or 55605 or 55606
                or 55607 or 55608 or 55609
                or 55618
                or 55701 or 55702 or 55703
                or 55704 =>
                    DocumentErrorCode.InvalidRequest,

            55002 or 55003 or 55004
                or 55005 or 55006 or 55007
                or 55103 or 55104 or 55105
                or 55106 or 55107 or 55108
                or 55206 or 55207 or 55208
                or 55209 or 55210 or 55211
                or 55212
                or 55303 or 55304 or 55305
                or 55306 or 55307 or 55308
                or 55403 or 55404 or 55405
                or 55406 or 55407 or 55409
                or 55410
                or 55518 or 55519 or 55520
                or 55521 or 55522 or 55523
                or 55610 or 55611 or 55612
                or 55613 or 55614 or 55615
                or 55705 or 55706 or 55707
                or 55708 or 55709 or 55710 =>
                    DocumentErrorCode.AccessNotAvailable,

            55109 or 55524 =>
                DocumentErrorCode.EmployeeNotFound,

            55525 or 55619 =>
                DocumentErrorCode.DocumentTypeNotFound,

            55408 or 55616 or 55711 =>
                DocumentErrorCode.EmployeeDocumentNotFound,

            55526 =>
                DocumentErrorCode.DuplicateStorageKey,

            55527 or 55617 or 55620
                or 55712 or 55713 =>
                    DocumentErrorCode.ConcurrencyConflict,

            _ =>
                DocumentErrorCode.None
        };

        return errorCode != DocumentErrorCode.None;
    }
}
