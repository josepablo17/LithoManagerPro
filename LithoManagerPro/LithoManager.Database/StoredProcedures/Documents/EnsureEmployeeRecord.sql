CREATE PROCEDURE [Documents].[EnsureEmployeeRecord]
    @EmployeeId int,
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

    IF @EmployeeId IS NULL
       OR @EmployeeId <= 0
    BEGIN
        THROW 55101,
            N'EmployeeId must be greater than zero.',
            1;
    END;

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 55102,
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

    DECLARE @IdentificationNumber nvarchar(30);
    DECLARE @FirstName nvarchar(100);
    DECLARE @LastName nvarchar(150);
    DECLARE @DepartmentId int;
    DECLARE @DepartmentCode nvarchar(50);
    DECLARE @DepartmentName nvarchar(100);
    DECLARE @EmployeeRecordId int;
    DECLARE @WasCreated bit = 0;

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
            THROW 55103,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0
        BEGIN
            THROW 55104,
                N'The actor user account is inactive.',
                1;
        END;

        IF @IsActorRoleActive = 0
        BEGIN
            THROW 55105,
                N'The actor role is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorEmployeeActive <> 1
        BEGIN
            THROW 55106,
                N'The actor employee record is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorDepartmentActive <> 1
        BEGIN
            THROW 55107,
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
            THROW 55108,
                N'The actor role is not allowed to ensure employee records.',
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
            THROW 55109,
                N'The employee was not found.',
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

            SET @WasCreated = 1;
        END;

        IF @WasCreated = 1
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

        COMMIT TRANSACTION;

        SELECT
            ER.[EmployeeRecordId],
            ER.[EmployeeId],
            @IdentificationNumber AS [IdentificationNumber],
            @FirstName AS [FirstName],
            @LastName AS [LastName],
            @DepartmentId AS [DepartmentId],
            @DepartmentCode AS [DepartmentCode],
            @DepartmentName AS [DepartmentName],
            ER.[CreatedAtUtc],
            ER.[CreatedByUserId],
            ER.[UpdatedAtUtc],
            ER.[UpdatedByUserId],
            ER.[RowVersion]
        FROM [Documents].[EmployeeRecords] AS ER
        WHERE ER.[EmployeeRecordId] = @EmployeeRecordId;
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
