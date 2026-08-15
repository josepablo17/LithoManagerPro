CREATE PROCEDURE [Documents].[SetEmployeeDocumentStatus]
    @EmployeeDocumentId int,
    @IsActive bit,
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

    IF @EmployeeDocumentId IS NULL
       OR @EmployeeDocumentId <= 0
    BEGIN
        THROW 55701,
            N'EmployeeDocumentId must be greater than zero.',
            1;
    END;

    IF @IsActive IS NULL
    BEGIN
        THROW 55702,
            N'IsActive is required.',
            1;
    END;

    IF @ExpectedRowVersion IS NULL
       OR DATALENGTH(@ExpectedRowVersion) <> 8
    BEGIN
        THROW 55703,
            N'ExpectedRowVersion must contain exactly 8 bytes.',
            1;
    END;

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 55704,
            N'The ActorUserId must be greater than zero.',
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
    DECLARE @DocumentTypeId int;
    DECLARE @DocumentTypeCode nvarchar(50);
    DECLARE @DocumentTypeName nvarchar(100);

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
    DECLARE @ExistingDeactivatedAtUtc datetime2(3);
    DECLARE @ExistingDeactivatedByUserId int;
    DECLARE @ExistingRowVersion varbinary(8);
    DECLARE @PreviousValuesJson nvarchar(max);

    DECLARE @ResultDocument TABLE
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
            THROW 55705,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0
        BEGIN
            THROW 55706,
                N'The actor user account is inactive.',
                1;
        END;

        IF @IsActorRoleActive = 0
        BEGIN
            THROW 55707,
                N'The actor role is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorEmployeeActive <> 1
        BEGIN
            THROW 55708,
                N'The actor employee record is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorDepartmentActive <> 1
        BEGIN
            THROW 55709,
                N'The actor department is inactive.',
                1;
        END;

        IF @ActorRoleCode NOT IN
        (
            N'SuperAdministrator',
            N'HumanResourcesAdministrator'
        )
        BEGIN
            THROW 55710,
                N'The actor role is not allowed to set employee document status.',
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
            @DocumentTypeId =
                ED.[DocumentTypeId],
            @DocumentTypeCode =
                DT.[DocumentTypeCode],
            @DocumentTypeName =
                DT.[Name],
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
            @ExistingDeactivatedAtUtc =
                ED.[DeactivatedAtUtc],
            @ExistingDeactivatedByUserId =
                ED.[DeactivatedByUserId],
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
        INNER JOIN [Documents].[DocumentTypes] AS DT
            ON DT.[DocumentTypeId] = ED.[DocumentTypeId]
        WHERE ED.[EmployeeDocumentId] = @EmployeeDocumentId;

        IF @EmployeeRecordId IS NULL
        BEGIN
            THROW 55711,
                N'The employee document was not found.',
                1;
        END;

        IF @ExistingRowVersion <> @ExpectedRowVersion
        BEGIN
            THROW 55712,
                N'The employee document has been modified by another transaction.',
                1;
        END;

        SET @PreviousValuesJson =
        (
            SELECT
                @EmployeeDocumentId AS [EmployeeDocumentId],
                @EmployeeId AS [EmployeeId],
                @DocumentTypeId AS [DocumentTypeId],
                @ExistingTitle AS [Title],
                @ExistingDescription AS [Description],
                @ExistingOriginalFileName AS [OriginalFileName],
                @ExistingContentType AS [ContentType],
                @ExistingFileSizeBytes AS [FileSizeBytes],
                @ExistingFileHashAlgorithm AS [FileHashAlgorithm],
                @ExistingIssuedDate AS [IssuedDate],
                @ExistingExpirationDate AS [ExpirationDate],
                @ExistingIsVisibleToEmployee AS [IsVisibleToEmployee],
                @ExistingIsActive AS [IsActive],
                @ExistingDeactivatedAtUtc AS [DeactivatedAtUtc],
                @ExistingDeactivatedByUserId AS [DeactivatedByUserId]
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        IF @ExistingIsActive = @IsActive
        BEGIN
            INSERT INTO @ResultDocument
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
            SELECT
                ED.[EmployeeDocumentId],
                ED.[EmployeeRecordId],
                ED.[DocumentTypeId],
                ED.[Title],
                ED.[Description],
                ED.[OriginalFileName],
                ED.[ContentType],
                ED.[FileSizeBytes],
                ED.[FileHashAlgorithm],
                ED.[IssuedDate],
                ED.[ExpirationDate],
                ED.[IsVisibleToEmployee],
                ED.[IsActive],
                ED.[DeactivatedAtUtc],
                ED.[DeactivatedByUserId],
                ED.[CreatedAtUtc],
                ED.[CreatedByUserId],
                ED.[UpdatedAtUtc],
                ED.[UpdatedByUserId],
                ED.[RowVersion]
            FROM [Documents].[EmployeeDocuments] AS ED
            WHERE ED.[EmployeeDocumentId] = @EmployeeDocumentId;
        END;
        ELSE
        BEGIN
            UPDATE [Documents].[EmployeeDocuments]
            SET
                [IsActive] =
                    @IsActive,
                [DeactivatedAtUtc] =
                    CASE
                        WHEN @IsActive = 0 THEN @OccurredAtUtc
                        ELSE NULL
                    END,
                [DeactivatedByUserId] =
                    CASE
                        WHEN @IsActive = 0 THEN @ActorUserId
                        ELSE NULL
                    END,
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
            INTO @ResultDocument
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
                THROW 55713,
                    N'The employee document status update returned an unexpected row count.',
                    1;
            END;
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
            N'EmployeeDocumentStatusSet',
            N'EmployeeDocuments',
            CONVERT(nvarchar(100), @EmployeeDocumentId),
            N'User',
            @ActorUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            CASE
                WHEN @ExistingIsActive = @IsActive
                    THEN N'Employee document status was already set.'
                ELSE N'Employee document status updated successfully.'
            END,
            @ClientIpAddress,
            @UserAgent,
            N'PATCH',
            @RequestPath,
            @PreviousValuesJson,
            (
                SELECT
                    RD.[EmployeeDocumentId],
                    @EmployeeId AS [EmployeeId],
                    RD.[DocumentTypeId],
                    RD.[Title],
                    RD.[Description],
                    RD.[OriginalFileName],
                    RD.[ContentType],
                    RD.[FileSizeBytes],
                    RD.[FileHashAlgorithm],
                    RD.[IssuedDate],
                    RD.[ExpirationDate],
                    RD.[IsVisibleToEmployee],
                    RD.[IsActive],
                    RD.[DeactivatedAtUtc],
                    RD.[DeactivatedByUserId]
                FROM @ResultDocument AS RD
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            ),
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        SELECT
            RD.[EmployeeDocumentId],
            RD.[EmployeeRecordId],
            @EmployeeId AS [EmployeeId],
            @IdentificationNumber AS [IdentificationNumber],
            @FirstName AS [FirstName],
            @LastName AS [LastName],
            @DepartmentId AS [DepartmentId],
            @DepartmentCode AS [DepartmentCode],
            @DepartmentName AS [DepartmentName],
            RD.[DocumentTypeId],
            @DocumentTypeCode AS [DocumentTypeCode],
            @DocumentTypeName AS [DocumentTypeName],
            RD.[Title],
            RD.[Description],
            RD.[OriginalFileName],
            RD.[ContentType],
            RD.[FileSizeBytes],
            RD.[FileHashAlgorithm],
            RD.[IssuedDate],
            RD.[ExpirationDate],
            RD.[IsVisibleToEmployee],
            RD.[IsActive],
            RD.[DeactivatedAtUtc],
            RD.[DeactivatedByUserId],
            RD.[CreatedAtUtc],
            RD.[CreatedByUserId],
            RD.[UpdatedAtUtc],
            RD.[UpdatedByUserId],
            RD.[RowVersion]
        FROM @ResultDocument AS RD;
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
