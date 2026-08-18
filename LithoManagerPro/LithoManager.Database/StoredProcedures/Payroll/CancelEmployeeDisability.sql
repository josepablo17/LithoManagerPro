CREATE PROCEDURE [Payroll].[CancelEmployeeDisability]
    @EmployeeDisabilityId int,
    @CancellationReason nvarchar(4000),
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
        COALESCE(@CorrelationId, NEWID());

    DECLARE @NormalizedCancellationReason nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@CancellationReason)), N'');

    IF @EmployeeDisabilityId IS NULL OR @EmployeeDisabilityId <= 0
    BEGIN
        THROW 56801,
            N'EmployeeDisabilityId must be greater than zero.',
            1;
    END;

    IF @NormalizedCancellationReason IS NULL
    BEGIN
        THROW 56802,
            N'CancellationReason is required.',
            1;
    END;

    IF LEN(@NormalizedCancellationReason) > 300
    BEGIN
        THROW 56803,
            N'CancellationReason cannot exceed 300 characters.',
            1;
    END;

    IF @ExpectedRowVersion IS NULL
       OR DATALENGTH(@ExpectedRowVersion) <> 8
    BEGIN
        THROW 56804,
            N'ExpectedRowVersion must contain exactly 8 bytes.',
            1;
    END;

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
    BEGIN
        THROW 56805,
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
    DECLARE @CurrentDisabilityStatus nvarchar(30);
    DECLARE @ExistingRowVersion varbinary(8);
    DECLARE @PreviousValuesJson nvarchar(max);

    DECLARE @ResultDisability TABLE
    (
        [EmployeeDisabilityId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [DisabilityTypeId] int NOT NULL,
        [IssuerInstitution] nvarchar(30) NOT NULL,
        [ReferenceNumber] nvarchar(100) NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [ReportedDate] date NOT NULL,
        [DisabilityStatus] nvarchar(30) NOT NULL,
        [EmployerPaidAmount] decimal(18,2) NULL,
        [SubsidyAmount] decimal(18,2) NULL,
        [ApprovedAtUtc] datetime2(3) NULL,
        [ApprovedByUserId] int NULL,
        [CancelledAtUtc] datetime2(3) NULL,
        [CancelledByUserId] int NULL,
        [CancellationReason] nvarchar(300) NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAtUtc] datetime2(3) NOT NULL,
        [CreatedByUserId] int NULL,
        [UpdatedAtUtc] datetime2(3) NULL,
        [UpdatedByUserId] int NULL,
        [RowVersion] varbinary(8) NOT NULL
    );

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            @ActorEmailAddress = U.[EmailAddress],
            @ActorRoleCode = R.[RoleCode],
            @IsActorUserActive = U.[IsActive],
            @IsActorRoleActive = R.[IsActive],
            @ActorEmployeeId = E.[EmployeeId],
            @IsActorEmployeeActive = E.[IsActive],
            @IsActorDepartmentActive = D.[IsActive]
        FROM [Security].[Users] AS U WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN [Security].[Roles] AS R
            ON R.[RoleId] = U.[RoleId]
        LEFT JOIN [HumanResources].[Employees] AS E
            ON E.[UserId] = U.[UserId]
        LEFT JOIN [HumanResources].[Departments] AS D
            ON D.[DepartmentId] = E.[DepartmentId]
        WHERE U.[UserId] = @ActorUserId;

        IF @ActorEmailAddress IS NULL
        BEGIN
            THROW 56806,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0 OR @IsActorRoleActive = 0
        BEGIN
            THROW 56807,
                N'The actor user or role is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND
           (
               @IsActorEmployeeActive <> 1
               OR @IsActorDepartmentActive <> 1
           )
        BEGIN
            THROW 56808,
                N'The actor employee or department is inactive.',
                1;
        END;

        IF @ActorRoleCode NOT IN
        (
            N'SuperAdministrator',
            N'HumanResourcesAdministrator'
        )
        BEGIN
            THROW 56809,
                N'The actor role is not allowed to cancel disability records.',
                1;
        END;

        SELECT
            @CurrentDisabilityStatus = ED.[DisabilityStatus],
            @ExistingRowVersion = ED.[RowVersion],
            @PreviousValuesJson =
            (
                SELECT
                    ED.[EmployeeDisabilityId],
                    ED.[EmployeeId],
                    ED.[DisabilityTypeId],
                    ED.[IssuerInstitution],
                    ED.[ReferenceNumber],
                    ED.[StartDate],
                    ED.[EndDate],
                    ED.[ReportedDate],
                    ED.[DisabilityStatus],
                    ED.[EmployerPaidAmount],
                    ED.[SubsidyAmount],
                    ED.[ApprovedAtUtc],
                    ED.[ApprovedByUserId],
                    ED.[Notes]
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            )
        FROM [Payroll].[EmployeeDisabilities] AS ED WITH (UPDLOCK, HOLDLOCK)
        WHERE ED.[EmployeeDisabilityId] = @EmployeeDisabilityId;

        IF @CurrentDisabilityStatus IS NULL
        BEGIN
            THROW 56810,
                N'The disability record was not found.',
                1;
        END;

        IF @ExistingRowVersion <> @ExpectedRowVersion
        BEGIN
            THROW 56811,
                N'The disability record has been modified by another transaction.',
                1;
        END;

        IF @CurrentDisabilityStatus = N'Cancelled'
        BEGIN
            THROW 56812,
                N'The disability record is already cancelled.',
                1;
        END;

        UPDATE [Payroll].[EmployeeDisabilities]
        SET
            [DisabilityStatus] = N'Cancelled',
            [ApprovedAtUtc] = NULL,
            [ApprovedByUserId] = NULL,
            [CancelledAtUtc] = @OccurredAtUtc,
            [CancelledByUserId] = @ActorUserId,
            [CancellationReason] = CONVERT(nvarchar(300), @NormalizedCancellationReason),
            [UpdatedAtUtc] = @OccurredAtUtc,
            [UpdatedByUserId] = @ActorUserId
        OUTPUT
            INSERTED.[EmployeeDisabilityId],
            INSERTED.[EmployeeId],
            INSERTED.[DisabilityTypeId],
            INSERTED.[IssuerInstitution],
            INSERTED.[ReferenceNumber],
            INSERTED.[StartDate],
            INSERTED.[EndDate],
            INSERTED.[ReportedDate],
            INSERTED.[DisabilityStatus],
            INSERTED.[EmployerPaidAmount],
            INSERTED.[SubsidyAmount],
            INSERTED.[ApprovedAtUtc],
            INSERTED.[ApprovedByUserId],
            INSERTED.[CancelledAtUtc],
            INSERTED.[CancelledByUserId],
            INSERTED.[CancellationReason],
            INSERTED.[Notes],
            INSERTED.[CreatedAtUtc],
            INSERTED.[CreatedByUserId],
            INSERTED.[UpdatedAtUtc],
            INSERTED.[UpdatedByUserId],
            INSERTED.[RowVersion]
        INTO @ResultDisability
        WHERE [EmployeeDisabilityId] = @EmployeeDisabilityId;

        IF @@ROWCOUNT <> 1
        BEGIN
            THROW 56813,
                N'The disability cancellation returned an unexpected row count.',
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
        SELECT
            @ResolvedCorrelationId,
            N'Payroll',
            N'EmployeeDisabilityCancelled',
            N'EmployeeDisabilities',
            CONVERT(nvarchar(100), R.[EmployeeDisabilityId]),
            N'User',
            @ActorUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            N'Employee disability record cancelled successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'PATCH',
            @RequestPath,
            @PreviousValuesJson,
            (
                SELECT R.*
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            ),
            @OccurredAtUtc
        FROM @ResultDisability AS R;

        COMMIT TRANSACTION;

        SELECT
            R.[EmployeeDisabilityId],
            R.[EmployeeId],
            E.[IdentificationType],
            E.[IdentificationNumber],
            E.[FirstName],
            E.[LastName],
            R.[DisabilityTypeId],
            DT.[DisabilityTypeCode],
            DT.[Name] AS [DisabilityTypeName],
            DT.[CountsAsSalaryForAguinaldo],
            DT.[RequiresSubsidyTracking],
            DT.[ReducesWorkedDays],
            R.[IssuerInstitution],
            R.[ReferenceNumber],
            R.[StartDate],
            R.[EndDate],
            R.[ReportedDate],
            R.[DisabilityStatus],
            R.[EmployerPaidAmount],
            R.[SubsidyAmount],
            R.[ApprovedAtUtc],
            R.[ApprovedByUserId],
            R.[CancelledAtUtc],
            R.[CancelledByUserId],
            R.[CancellationReason],
            R.[Notes],
            R.[CreatedAtUtc],
            R.[CreatedByUserId],
            R.[UpdatedAtUtc],
            R.[UpdatedByUserId],
            R.[RowVersion]
        FROM @ResultDisability AS R
        INNER JOIN [HumanResources].[Employees] AS E
            ON E.[EmployeeId] = R.[EmployeeId]
        INNER JOIN [Payroll].[DisabilityTypes] AS DT
            ON DT.[DisabilityTypeId] = R.[DisabilityTypeId];
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
