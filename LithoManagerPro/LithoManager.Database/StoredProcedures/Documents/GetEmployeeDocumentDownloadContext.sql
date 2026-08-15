CREATE PROCEDURE [Documents].[GetEmployeeDocumentDownloadContext]
    @EmployeeDocumentId int,
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
        THROW 55401,
            N'EmployeeDocumentId must be greater than zero.',
            1;
    END;

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 55402,
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

    DECLARE @DocumentEmployeeId int;
    DECLARE @IsDocumentActive bit;
    DECLARE @IsVisibleToEmployee bit;
    DECLARE @Title nvarchar(150);

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
            THROW 55403,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0
        BEGIN
            THROW 55404,
                N'The actor user account is inactive.',
                1;
        END;

        IF @IsActorRoleActive = 0
        BEGIN
            THROW 55405,
                N'The actor role is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorEmployeeActive <> 1
        BEGIN
            THROW 55406,
                N'The actor employee record is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorDepartmentActive <> 1
        BEGIN
            THROW 55407,
                N'The actor department is inactive.',
                1;
        END;

        SELECT
            @DocumentEmployeeId =
                ER.[EmployeeId],
            @IsDocumentActive =
                ED.[IsActive],
            @IsVisibleToEmployee =
                ED.[IsVisibleToEmployee],
            @Title =
                ED.[Title]
        FROM [Documents].[EmployeeDocuments] AS ED
            WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN [Documents].[EmployeeRecords] AS ER
            ON ER.[EmployeeRecordId] = ED.[EmployeeRecordId]
        WHERE ED.[EmployeeDocumentId] = @EmployeeDocumentId;

        IF @DocumentEmployeeId IS NULL
        BEGIN
            THROW 55408,
                N'The employee document was not found.',
                1;
        END;

        IF @ActorRoleCode NOT IN
        (
            N'SuperAdministrator',
            N'HumanResourcesAdministrator',
            N'HumanResourcesStaff'
        )
        BEGIN
            IF @ActorEmployeeId IS NULL
            BEGIN
                THROW 55409,
                    N'The actor user is not linked to an employee.',
                    1;
            END;

            IF @DocumentEmployeeId <> @ActorEmployeeId
               OR @IsDocumentActive <> 1
               OR @IsVisibleToEmployee <> 1
            BEGIN
                THROW 55410,
                    N'The actor is not allowed to download this document.',
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
            [NewValuesJson],
            [OccurredAtUtc]
        )
        VALUES
        (
            @ResolvedCorrelationId,
            N'Documents',
            N'EmployeeDocumentDownloaded',
            N'EmployeeDocuments',
            CONVERT(nvarchar(100), @EmployeeDocumentId),
            N'User',
            @ActorUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            N'Employee document download context requested.',
            @ClientIpAddress,
            @UserAgent,
            N'GET',
            @RequestPath,
            (
                SELECT
                    @EmployeeDocumentId AS [EmployeeDocumentId],
                    @DocumentEmployeeId AS [EmployeeId],
                    @Title AS [Title]
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            ),
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        SELECT
            ED.[EmployeeDocumentId],
            ER.[EmployeeRecordId],
            ER.[EmployeeId],
            E.[IdentificationNumber],
            E.[FirstName],
            E.[LastName],
            ED.[DocumentTypeId],
            DT.[DocumentTypeCode],
            DT.[Name] AS [DocumentTypeName],
            ED.[Title],
            ED.[OriginalFileName],
            ED.[StorageProvider],
            ED.[StorageKey],
            ED.[ContentType],
            ED.[FileSizeBytes],
            ED.[FileHash],
            ED.[FileHashAlgorithm],
            ED.[IsVisibleToEmployee],
            ED.[IsActive],
            ED.[RowVersion]
        FROM [Documents].[EmployeeDocuments] AS ED
        INNER JOIN [Documents].[EmployeeRecords] AS ER
            ON ER.[EmployeeRecordId] = ED.[EmployeeRecordId]
        INNER JOIN [HumanResources].[Employees] AS E
            ON E.[EmployeeId] = ER.[EmployeeId]
        INNER JOIN [Documents].[DocumentTypes] AS DT
            ON DT.[DocumentTypeId] = ED.[DocumentTypeId]
        WHERE ED.[EmployeeDocumentId] = @EmployeeDocumentId;
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
