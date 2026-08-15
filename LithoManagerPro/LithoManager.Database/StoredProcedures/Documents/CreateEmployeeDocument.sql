CREATE PROCEDURE [Documents].[CreateEmployeeDocument]
    @EmployeeId int,
    @DocumentTypeId int,
    @Title nvarchar(4000),
    @Description nvarchar(4000) = NULL,
    @OriginalFileName nvarchar(4000),
    @StorageProvider nvarchar(4000),
    @StorageKey nvarchar(4000),
    @ContentType nvarchar(4000),
    @FileSizeBytes bigint,
    @FileHash varbinary(32),
    @IssuedDate date = NULL,
    @ExpirationDate date = NULL,
    @IsVisibleToEmployee bit = NULL,
    @ActorUserId int,
    @CorrelationId uniqueidentifier = NULL,
    @ClientIpAddress nvarchar(45) = NULL,
    @UserAgent nvarchar(512) = NULL,
    @RequestPath nvarchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @OccurredAtUtc datetime2(3) =
        SYSUTCDATETIME();

    DECLARE @ResolvedCorrelationId uniqueidentifier =
        COALESCE(
            @CorrelationId,
            NEWID()
        );

    DECLARE @NormalizedTitle nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@Title)), N'');

    DECLARE @NormalizedDescription nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@Description)), N'');

    DECLARE @NormalizedOriginalFileName nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@OriginalFileName)), N'');

    DECLARE @NormalizedStorageProvider nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@StorageProvider)), N'');

    DECLARE @NormalizedStorageKey nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@StorageKey)), N'');

    DECLARE @NormalizedContentType nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@ContentType)), N'');

    IF @EmployeeId IS NULL
       OR @EmployeeId <= 0
    BEGIN
        THROW 55501,
            N'EmployeeId must be greater than zero.',
            1;
    END;

    IF @DocumentTypeId IS NULL
       OR @DocumentTypeId <= 0
    BEGIN
        THROW 55502,
            N'DocumentTypeId must be greater than zero.',
            1;
    END;

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 55503,
            N'The ActorUserId must be greater than zero.',
            1;
    END;

    IF @NormalizedTitle IS NULL
    BEGIN
        THROW 55504,
            N'Title is required.',
            1;
    END;

    IF LEN(@NormalizedTitle) > 150
    BEGIN
        THROW 55505,
            N'Title cannot exceed 150 characters.',
            1;
    END;

    IF @NormalizedDescription IS NOT NULL
       AND LEN(@NormalizedDescription) > 500
    BEGIN
        THROW 55506,
            N'Description cannot exceed 500 characters.',
            1;
    END;

    IF @NormalizedOriginalFileName IS NULL
    BEGIN
        THROW 55507,
            N'OriginalFileName is required.',
            1;
    END;

    IF LEN(@NormalizedOriginalFileName) > 260
    BEGIN
        THROW 55508,
            N'OriginalFileName cannot exceed 260 characters.',
            1;
    END;

    IF @NormalizedStorageProvider IS NULL
    BEGIN
        THROW 55509,
            N'StorageProvider is required.',
            1;
    END;

    IF LEN(@NormalizedStorageProvider) > 50
    BEGIN
        THROW 55510,
            N'StorageProvider cannot exceed 50 characters.',
            1;
    END;

    IF @NormalizedStorageKey IS NULL
    BEGIN
        THROW 55511,
            N'StorageKey is required.',
            1;
    END;

    IF LEN(@NormalizedStorageKey) > 450
    BEGIN
        THROW 55512,
            N'StorageKey cannot exceed 450 characters.',
            1;
    END;

    IF @NormalizedContentType IS NULL
    BEGIN
        THROW 55513,
            N'ContentType is required.',
            1;
    END;

    IF LEN(@NormalizedContentType) > 150
    BEGIN
        THROW 55514,
            N'ContentType cannot exceed 150 characters.',
            1;
    END;

    IF @FileSizeBytes IS NULL
       OR @FileSizeBytes <= 0
    BEGIN
        THROW 55515,
            N'FileSizeBytes must be greater than zero.',
            1;
    END;

    IF @FileHash IS NULL
       OR DATALENGTH(@FileHash) <> 32
    BEGIN
        THROW 55516,
            N'FileHash must contain exactly 32 bytes.',
            1;
    END;

    IF @ExpirationDate IS NOT NULL
       AND @IssuedDate IS NOT NULL
       AND @ExpirationDate < @IssuedDate
    BEGIN
        THROW 55517,
            N'ExpirationDate cannot be earlier than IssuedDate.',
            1;
    END;

    DECLARE @ActorEmailAddress nvarchar(254);
    DECLARE @ActorRoleCode nvarchar(50);
    DECLARE @IsActorUserActive bit;
    DECLARE @IsActorRoleActive bit;
    DECLARE @ActorEmployeeId int;
    DECLARE @IsActorEmployeeActive bit;
    DECLARE @IsActorDepartmentActive bit;

    DECLARE @IdentificationNumber nvarchar(30);
    DECLARE @FirstName nvarchar(100);
    DECLARE @LastName nvarchar(150);
    DECLARE @DepartmentId int;
    DECLARE @DepartmentCode nvarchar(50);
    DECLARE @DepartmentName nvarchar(100);
    DECLARE @DocumentTypeCode nvarchar(50);
    DECLARE @DocumentTypeName nvarchar(100);
    DECLARE @DefaultIsVisibleToEmployee bit;
    DECLARE @EmployeeRecordId int;
    DECLARE @EmployeeDocumentId int;
    DECLARE @WasRecordCreated bit = 0;

    DECLARE @CreatedDocument TABLE
    (
        [EmployeeDocumentId] int NOT NULL,
        [EmployeeRecordId] int NOT NULL,
        [DocumentTypeId] int NOT NULL,
        [Title] nvarchar(150) NOT NULL,
        [Description] nvarchar(500) NULL,
        [OriginalFileName] nvarchar(260) NOT NULL,
        [ContentType] nvarchar(150) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [FileHashAlgorithm] nvarchar(20) NOT NULL,
        [IssuedDate] date NULL,
        [ExpirationDate] date NULL,
        [IsVisibleToEmployee] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [DeactivatedAtUtc] datetime2(3) NULL,
        [DeactivatedByUserId] int NULL,
        [CreatedAtUtc] datetime2(3) NOT NULL,
        [CreatedByUserId] int NOT NULL,
        [UpdatedAtUtc] datetime2(3) NULL,
        [UpdatedByUserId] int NULL,
        [RowVersion] varbinary(8) NOT NULL
    );

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            @ActorEmailAddress =
                U.[EmailAddress],
            @ActorRoleCode =
                R.[RoleCode],
            @IsActorUserActive =
                U.[IsActive],
            @IsActorRoleActive =
                R.[IsActive],
            @ActorEmployeeId =
                E.[EmployeeId],
            @IsActorEmployeeActive =
                E.[IsActive],
            @IsActorDepartmentActive =
                D.[IsActive]
        FROM [Security].[Users] AS U
            WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN [Security].[Roles] AS R
            ON R.[RoleId] = U.[RoleId]
        LEFT JOIN [HumanResources].[Employees] AS E
            ON E.[UserId] = U.[UserId]
        LEFT JOIN [HumanResources].[Departments] AS D
            ON D.[DepartmentId] = E.[DepartmentId]
        WHERE U.[UserId] = @ActorUserId;

        IF @ActorEmailAddress IS NULL
        BEGIN
            THROW 55518,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0
        BEGIN
            THROW 55519,
                N'The actor user account is inactive.',
                1;
        END;

        IF @IsActorRoleActive = 0
        BEGIN
            THROW 55520,
                N'The actor role is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorEmployeeActive <> 1
        BEGIN
            THROW 55521,
                N'The actor employee record is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorDepartmentActive <> 1
        BEGIN
            THROW 55522,
                N'The actor department is inactive.',
                1;
        END;

        IF @ActorRoleCode NOT IN
        (
            N'SuperAdministrator',
            N'HumanResourcesAdministrator',
            N'HumanResourcesStaff'
        )
        BEGIN
            THROW 55523,
                N'The actor role is not allowed to create employee documents.',
                1;
        END;

        SELECT
            @IdentificationNumber =
                E.[IdentificationNumber],
            @FirstName =
                E.[FirstName],
            @LastName =
                E.[LastName],
            @DepartmentId =
                D.[DepartmentId],
            @DepartmentCode =
                D.[DepartmentCode],
            @DepartmentName =
                D.[Name]
        FROM [HumanResources].[Employees] AS E
            WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN [HumanResources].[Departments] AS D
            ON D.[DepartmentId] = E.[DepartmentId]
        WHERE E.[EmployeeId] = @EmployeeId;

        IF @IdentificationNumber IS NULL
        BEGIN
            THROW 55524,
                N'The employee was not found.',
                1;
        END;

        SELECT
            @DocumentTypeCode =
                DT.[DocumentTypeCode],
            @DocumentTypeName =
                DT.[Name],
            @DefaultIsVisibleToEmployee =
                DT.[DefaultIsVisibleToEmployee]
        FROM [Documents].[DocumentTypes] AS DT
            WITH (UPDLOCK, HOLDLOCK)
        WHERE DT.[DocumentTypeId] = @DocumentTypeId
          AND DT.[IsActive] = 1;

        IF @DocumentTypeCode IS NULL
        BEGIN
            THROW 55525,
                N'The active document type was not found.',
                1;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM [Documents].[EmployeeDocuments] AS ED
                WITH (UPDLOCK, HOLDLOCK)
            WHERE ED.[StorageKey] = @NormalizedStorageKey
        )
        BEGIN
            THROW 55526,
                N'An employee document with the same StorageKey already exists.',
                1;
        END;

        SELECT
            @EmployeeRecordId =
                ER.[EmployeeRecordId]
        FROM [Documents].[EmployeeRecords] AS ER
            WITH (UPDLOCK, HOLDLOCK)
        WHERE ER.[EmployeeId] = @EmployeeId;

        IF @EmployeeRecordId IS NULL
        BEGIN
            INSERT INTO [Documents].[EmployeeRecords]
            (
                [EmployeeId],
                [CreatedByUserId]
            )
            VALUES
            (
                @EmployeeId,
                @ActorUserId
            );

            SET @EmployeeRecordId =
                CONVERT(int, SCOPE_IDENTITY());

            SET @WasRecordCreated = 1;
        END;

        INSERT INTO [Documents].[EmployeeDocuments]
        (
            [EmployeeRecordId],
            [DocumentTypeId],
            [Title],
            [Description],
            [OriginalFileName],
            [StorageProvider],
            [StorageKey],
            [ContentType],
            [FileSizeBytes],
            [FileHash],
            [IssuedDate],
            [ExpirationDate],
            [IsVisibleToEmployee],
            [CreatedByUserId]
        )
        OUTPUT
            INSERTED.[EmployeeDocumentId],
            INSERTED.[EmployeeRecordId],
            INSERTED.[DocumentTypeId],
            INSERTED.[Title],
            INSERTED.[Description],
            INSERTED.[OriginalFileName],
            INSERTED.[ContentType],
            INSERTED.[FileSizeBytes],
            INSERTED.[FileHashAlgorithm],
            INSERTED.[IssuedDate],
            INSERTED.[ExpirationDate],
            INSERTED.[IsVisibleToEmployee],
            INSERTED.[IsActive],
            INSERTED.[DeactivatedAtUtc],
            INSERTED.[DeactivatedByUserId],
            INSERTED.[CreatedAtUtc],
            INSERTED.[CreatedByUserId],
            INSERTED.[UpdatedAtUtc],
            INSERTED.[UpdatedByUserId],
            INSERTED.[RowVersion]
        INTO @CreatedDocument
        (
            [EmployeeDocumentId],
            [EmployeeRecordId],
            [DocumentTypeId],
            [Title],
            [Description],
            [OriginalFileName],
            [ContentType],
            [FileSizeBytes],
            [FileHashAlgorithm],
            [IssuedDate],
            [ExpirationDate],
            [IsVisibleToEmployee],
            [IsActive],
            [DeactivatedAtUtc],
            [DeactivatedByUserId],
            [CreatedAtUtc],
            [CreatedByUserId],
            [UpdatedAtUtc],
            [UpdatedByUserId],
            [RowVersion]
        )
        VALUES
        (
            @EmployeeRecordId,
            @DocumentTypeId,
            @NormalizedTitle,
            @NormalizedDescription,
            @NormalizedOriginalFileName,
            @NormalizedStorageProvider,
            @NormalizedStorageKey,
            @NormalizedContentType,
            @FileSizeBytes,
            @FileHash,
            @IssuedDate,
            @ExpirationDate,
            COALESCE(@IsVisibleToEmployee, @DefaultIsVisibleToEmployee),
            @ActorUserId
        );

        IF @@ROWCOUNT <> 1
        BEGIN
            THROW 55527,
                N'The employee document insert returned an unexpected row count.',
                1;
        END;

        SELECT
            @EmployeeDocumentId =
                CD.[EmployeeDocumentId]
        FROM @CreatedDocument AS CD;

        IF @WasRecordCreated = 1
        BEGIN
            INSERT INTO [Audit].[AuditLogs]
            (
                [CorrelationId],
                [ModuleName],
                [ActionName],
                [EntityName],
                [EntityId],
                [ActorType],
                [ActorUserId],
                [ActorEmailAddress],
                [ActorRoleCode],
                [IsSuccessful],
                [EventDescription],
                [ClientIpAddress],
                [UserAgent],
                [HttpMethod],
                [RequestPath],
                [NewValuesJson],
                [OccurredAtUtc]
            )
            VALUES
            (
                @ResolvedCorrelationId,
                N'Documents',
                N'EmployeeRecordCreated',
                N'EmployeeRecords',
                CONVERT(nvarchar(100), @EmployeeRecordId),
                N'User',
                @ActorUserId,
                @ActorEmailAddress,
                @ActorRoleCode,
                1,
                N'Employee record created successfully.',
                @ClientIpAddress,
                @UserAgent,
                N'POST',
                @RequestPath,
                (
                    SELECT
                        @EmployeeRecordId AS [EmployeeRecordId],
                        @EmployeeId AS [EmployeeId]
                    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                ),
                @OccurredAtUtc
            );
        END;

        INSERT INTO [Audit].[AuditLogs]
        (
            [CorrelationId],
            [ModuleName],
            [ActionName],
            [EntityName],
            [EntityId],
            [ActorType],
            [ActorUserId],
            [ActorEmailAddress],
            [ActorRoleCode],
            [IsSuccessful],
            [EventDescription],
            [ClientIpAddress],
            [UserAgent],
            [HttpMethod],
            [RequestPath],
            [NewValuesJson],
            [OccurredAtUtc]
        )
        VALUES
        (
            @ResolvedCorrelationId,
            N'Documents',
            N'EmployeeDocumentCreated',
            N'EmployeeDocuments',
            CONVERT(nvarchar(100), @EmployeeDocumentId),
            N'User',
            @ActorUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            N'Employee document created successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'POST',
            @RequestPath,
            (
                SELECT
                    CD.[EmployeeDocumentId],
                    @EmployeeId AS [EmployeeId],
                    CD.[DocumentTypeId],
                    CD.[Title],
                    CD.[Description],
                    CD.[OriginalFileName],
                    CD.[ContentType],
                    CD.[FileSizeBytes],
                    CD.[IssuedDate],
                    CD.[ExpirationDate],
                    CD.[IsVisibleToEmployee],
                    CD.[IsActive]
                FROM @CreatedDocument AS CD
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            ),
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        SELECT
            CD.[EmployeeDocumentId],
            CD.[EmployeeRecordId],
            @EmployeeId AS [EmployeeId],
            @IdentificationNumber AS [IdentificationNumber],
            @FirstName AS [FirstName],
            @LastName AS [LastName],
            @DepartmentId AS [DepartmentId],
            @DepartmentCode AS [DepartmentCode],
            @DepartmentName AS [DepartmentName],
            CD.[DocumentTypeId],
            @DocumentTypeCode AS [DocumentTypeCode],
            @DocumentTypeName AS [DocumentTypeName],
            CD.[Title],
            CD.[Description],
            CD.[OriginalFileName],
            CD.[ContentType],
            CD.[FileSizeBytes],
            CD.[FileHashAlgorithm],
            CD.[IssuedDate],
            CD.[ExpirationDate],
            CD.[IsVisibleToEmployee],
            CD.[IsActive],
            CD.[DeactivatedAtUtc],
            CD.[DeactivatedByUserId],
            CD.[CreatedAtUtc],
            CD.[CreatedByUserId],
            CD.[UpdatedAtUtc],
            CD.[UpdatedByUserId],
            CD.[RowVersion]
        FROM @CreatedDocument AS CD;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        THROW;
    END CATCH;
END;
GO
