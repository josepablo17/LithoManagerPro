CREATE PROCEDURE [Documents].[UpdateEmployeeDocument]
    @EmployeeDocumentId int,
    @DocumentTypeId int,
    @Title nvarchar(4000),
    @Description nvarchar(4000) = NULL,
    @IssuedDate date = NULL,
    @ExpirationDate date = NULL,
    @IsVisibleToEmployee bit,
    @ExpectedRowVersion varbinary(8),
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

    IF @EmployeeDocumentId IS NULL
       OR @EmployeeDocumentId <= 0
    BEGIN
        THROW 55601,
            N'EmployeeDocumentId must be greater than zero.',
            1;
    END;

    IF @DocumentTypeId IS NULL
       OR @DocumentTypeId <= 0
    BEGIN
        THROW 55602,
            N'DocumentTypeId must be greater than zero.',
            1;
    END;

    IF @IsVisibleToEmployee IS NULL
    BEGIN
        THROW 55603,
            N'IsVisibleToEmployee is required.',
            1;
    END;

    IF @ExpectedRowVersion IS NULL
       OR DATALENGTH(@ExpectedRowVersion) <> 8
    BEGIN
        THROW 55604,
            N'ExpectedRowVersion must contain exactly 8 bytes.',
            1;
    END;

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 55605,
            N'The ActorUserId must be greater than zero.',
            1;
    END;

    IF @NormalizedTitle IS NULL
    BEGIN
        THROW 55606,
            N'Title is required.',
            1;
    END;

    IF LEN(@NormalizedTitle) > 150
    BEGIN
        THROW 55607,
            N'Title cannot exceed 150 characters.',
            1;
    END;

    IF @NormalizedDescription IS NOT NULL
       AND LEN(@NormalizedDescription) > 500
    BEGIN
        THROW 55608,
            N'Description cannot exceed 500 characters.',
            1;
    END;

    IF @ExpirationDate IS NOT NULL
       AND @IssuedDate IS NOT NULL
       AND @ExpirationDate < @IssuedDate
    BEGIN
        THROW 55609,
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

    DECLARE @EmployeeRecordId int;
    DECLARE @EmployeeId int;
    DECLARE @IdentificationNumber nvarchar(30);
    DECLARE @FirstName nvarchar(100);
    DECLARE @LastName nvarchar(150);
    DECLARE @DepartmentId int;
    DECLARE @DepartmentCode nvarchar(50);
    DECLARE @DepartmentName nvarchar(100);
    DECLARE @DocumentTypeCode nvarchar(50);
    DECLARE @DocumentTypeName nvarchar(100);

    DECLARE @ExistingDocumentTypeId int;
    DECLARE @ExistingTitle nvarchar(150);
    DECLARE @ExistingDescription nvarchar(500);
    DECLARE @ExistingOriginalFileName nvarchar(260);
    DECLARE @ExistingContentType nvarchar(150);
    DECLARE @ExistingFileSizeBytes bigint;
    DECLARE @ExistingFileHashAlgorithm nvarchar(20);
    DECLARE @ExistingIssuedDate date;
    DECLARE @ExistingExpirationDate date;
    DECLARE @ExistingIsVisibleToEmployee bit;
    DECLARE @ExistingIsActive bit;
    DECLARE @ExistingRowVersion varbinary(8);
    DECLARE @PreviousValuesJson nvarchar(max);

    DECLARE @UpdatedDocument TABLE
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
            THROW 55610,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0
        BEGIN
            THROW 55611,
                N'The actor user account is inactive.',
                1;
        END;

        IF @IsActorRoleActive = 0
        BEGIN
            THROW 55612,
                N'The actor role is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorEmployeeActive <> 1
        BEGIN
            THROW 55613,
                N'The actor employee record is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorDepartmentActive <> 1
        BEGIN
            THROW 55614,
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
            THROW 55615,
                N'The actor role is not allowed to update employee documents.',
                1;
        END;

        SELECT
            @EmployeeRecordId =
                ED.[EmployeeRecordId],
            @EmployeeId =
                ER.[EmployeeId],
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
                D.[Name],
            @ExistingDocumentTypeId =
                ED.[DocumentTypeId],
            @ExistingTitle =
                ED.[Title],
            @ExistingDescription =
                ED.[Description],
            @ExistingOriginalFileName =
                ED.[OriginalFileName],
            @ExistingContentType =
                ED.[ContentType],
            @ExistingFileSizeBytes =
                ED.[FileSizeBytes],
            @ExistingFileHashAlgorithm =
                ED.[FileHashAlgorithm],
            @ExistingIssuedDate =
                ED.[IssuedDate],
            @ExistingExpirationDate =
                ED.[ExpirationDate],
            @ExistingIsVisibleToEmployee =
                ED.[IsVisibleToEmployee],
            @ExistingIsActive =
                ED.[IsActive],
            @ExistingRowVersion =
                ED.[RowVersion]
        FROM [Documents].[EmployeeDocuments] AS ED
            WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN [Documents].[EmployeeRecords] AS ER
            ON ER.[EmployeeRecordId] = ED.[EmployeeRecordId]
        INNER JOIN [HumanResources].[Employees] AS E
            ON E.[EmployeeId] = ER.[EmployeeId]
        INNER JOIN [HumanResources].[Departments] AS D
            ON D.[DepartmentId] = E.[DepartmentId]
        WHERE ED.[EmployeeDocumentId] = @EmployeeDocumentId;

        IF @EmployeeRecordId IS NULL
        BEGIN
            THROW 55616,
                N'The employee document was not found.',
                1;
        END;

        IF @ExistingRowVersion <> @ExpectedRowVersion
        BEGIN
            THROW 55617,
                N'The employee document has been modified by another transaction.',
                1;
        END;

        IF @ExistingIsActive <> 1
        BEGIN
            THROW 55618,
                N'Inactive employee documents cannot be updated.',
                1;
        END;

        SELECT
            @DocumentTypeCode =
                DT.[DocumentTypeCode],
            @DocumentTypeName =
                DT.[Name]
        FROM [Documents].[DocumentTypes] AS DT
            WITH (UPDLOCK, HOLDLOCK)
        WHERE DT.[DocumentTypeId] = @DocumentTypeId
          AND DT.[IsActive] = 1;

        IF @DocumentTypeCode IS NULL
        BEGIN
            THROW 55619,
                N'The active document type was not found.',
                1;
        END;

        SET @PreviousValuesJson =
        (
            SELECT
                @EmployeeDocumentId AS [EmployeeDocumentId],
                @EmployeeId AS [EmployeeId],
                @ExistingDocumentTypeId AS [DocumentTypeId],
                @ExistingTitle AS [Title],
                @ExistingDescription AS [Description],
                @ExistingOriginalFileName AS [OriginalFileName],
                @ExistingContentType AS [ContentType],
                @ExistingFileSizeBytes AS [FileSizeBytes],
                @ExistingFileHashAlgorithm AS [FileHashAlgorithm],
                @ExistingIssuedDate AS [IssuedDate],
                @ExistingExpirationDate AS [ExpirationDate],
                @ExistingIsVisibleToEmployee AS [IsVisibleToEmployee],
                @ExistingIsActive AS [IsActive]
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        UPDATE [Documents].[EmployeeDocuments]
        SET
            [DocumentTypeId] =
                @DocumentTypeId,
            [Title] =
                @NormalizedTitle,
            [Description] =
                @NormalizedDescription,
            [IssuedDate] =
                @IssuedDate,
            [ExpirationDate] =
                @ExpirationDate,
            [IsVisibleToEmployee] =
                @IsVisibleToEmployee,
            [UpdatedAtUtc] =
                @OccurredAtUtc,
            [UpdatedByUserId] =
                @ActorUserId
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
        INTO @UpdatedDocument
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
        WHERE [EmployeeDocumentId] = @EmployeeDocumentId;

        IF @@ROWCOUNT <> 1
        BEGIN
            THROW 55620,
                N'The employee document update returned an unexpected row count.',
                1;
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
            [PreviousValuesJson],
            [NewValuesJson],
            [OccurredAtUtc]
        )
        VALUES
        (
            @ResolvedCorrelationId,
            N'Documents',
            N'EmployeeDocumentUpdated',
            N'EmployeeDocuments',
            CONVERT(nvarchar(100), @EmployeeDocumentId),
            N'User',
            @ActorUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            N'Employee document updated successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'PUT',
            @RequestPath,
            @PreviousValuesJson,
            (
                SELECT
                    UD.[EmployeeDocumentId],
                    @EmployeeId AS [EmployeeId],
                    UD.[DocumentTypeId],
                    UD.[Title],
                    UD.[Description],
                    UD.[OriginalFileName],
                    UD.[ContentType],
                    UD.[FileSizeBytes],
                    UD.[FileHashAlgorithm],
                    UD.[IssuedDate],
                    UD.[ExpirationDate],
                    UD.[IsVisibleToEmployee],
                    UD.[IsActive]
                FROM @UpdatedDocument AS UD
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            ),
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        SELECT
            UD.[EmployeeDocumentId],
            UD.[EmployeeRecordId],
            @EmployeeId AS [EmployeeId],
            @IdentificationNumber AS [IdentificationNumber],
            @FirstName AS [FirstName],
            @LastName AS [LastName],
            @DepartmentId AS [DepartmentId],
            @DepartmentCode AS [DepartmentCode],
            @DepartmentName AS [DepartmentName],
            UD.[DocumentTypeId],
            @DocumentTypeCode AS [DocumentTypeCode],
            @DocumentTypeName AS [DocumentTypeName],
            UD.[Title],
            UD.[Description],
            UD.[OriginalFileName],
            UD.[ContentType],
            UD.[FileSizeBytes],
            UD.[FileHashAlgorithm],
            UD.[IssuedDate],
            UD.[ExpirationDate],
            UD.[IsVisibleToEmployee],
            UD.[IsActive],
            UD.[DeactivatedAtUtc],
            UD.[DeactivatedByUserId],
            UD.[CreatedAtUtc],
            UD.[CreatedByUserId],
            UD.[UpdatedAtUtc],
            UD.[UpdatedByUserId],
            UD.[RowVersion]
        FROM @UpdatedDocument AS UD;
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
