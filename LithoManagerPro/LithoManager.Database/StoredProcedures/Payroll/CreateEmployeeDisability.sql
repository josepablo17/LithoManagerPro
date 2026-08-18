CREATE PROCEDURE [Payroll].[CreateEmployeeDisability]
    @EmployeeId int,
    @DisabilityTypeId int,
    @IssuerInstitution nvarchar(4000),
    @StartDate date,
    @EndDate date,
    @ReferenceNumber nvarchar(4000) = NULL,
    @EmployerPaidAmount decimal(18,2) = NULL,
    @SubsidyAmount decimal(18,2) = NULL,
    @Notes nvarchar(4000) = NULL,
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

    DECLARE @ReportedDate date =
        CONVERT(date, @OccurredAtUtc);

    DECLARE @ResolvedCorrelationId uniqueidentifier =
        COALESCE(@CorrelationId, NEWID());

    DECLARE @NormalizedIssuerInstitution nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@IssuerInstitution)), N'');

    DECLARE @NormalizedReferenceNumber nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@ReferenceNumber)), N'');

    DECLARE @NormalizedNotes nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@Notes)), N'');

    IF @EmployeeId IS NULL OR @EmployeeId <= 0
    BEGIN
        THROW 56401,
            N'EmployeeId must be greater than zero.',
            1;
    END;

    IF @DisabilityTypeId IS NULL OR @DisabilityTypeId <= 0
    BEGIN
        THROW 56402,
            N'DisabilityTypeId must be greater than zero.',
            1;
    END;

    IF @NormalizedIssuerInstitution NOT IN
    (
        N'CCSS',
        N'INS',
        N'Employer',
        N'Other'
    )
    BEGIN
        THROW 56403,
            N'IssuerInstitution is invalid.',
            1;
    END;

    IF @StartDate IS NULL
       OR @EndDate IS NULL
       OR @EndDate < @StartDate
    BEGIN
        THROW 56404,
            N'The disability date range is invalid.',
            1;
    END;

    IF @NormalizedReferenceNumber IS NOT NULL
       AND LEN(@NormalizedReferenceNumber) > 100
    BEGIN
        THROW 56405,
            N'ReferenceNumber cannot exceed 100 characters.',
            1;
    END;

    IF (@EmployerPaidAmount IS NOT NULL AND @EmployerPaidAmount < 0)
       OR (@SubsidyAmount IS NOT NULL AND @SubsidyAmount < 0)
    BEGIN
        THROW 56406,
            N'Disability amounts cannot be negative.',
            1;
    END;

    IF @NormalizedNotes IS NOT NULL
       AND LEN(@NormalizedNotes) > 500
    BEGIN
        THROW 56407,
            N'Notes cannot exceed 500 characters.',
            1;
    END;

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
    BEGIN
        THROW 56408,
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
    DECLARE @IsEmployeeActive bit;
    DECLARE @IsEmployeeDepartmentActive bit;
    DECLARE @DisabilityTypeCode nvarchar(50);
    DECLARE @DisabilityTypeName nvarchar(100);
    DECLARE @IsDisabilityTypeActive bit;

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
            THROW 56409,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0 OR @IsActorRoleActive = 0
        BEGIN
            THROW 56410,
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
            THROW 56411,
                N'The actor employee or department is inactive.',
                1;
        END;

        IF @ActorRoleCode NOT IN
        (
            N'SuperAdministrator',
            N'HumanResourcesAdministrator',
            N'HumanResourcesStaff'
        )
        BEGIN
            THROW 56412,
                N'The actor role is not allowed to create disability records.',
                1;
        END;

        SELECT
            @IsEmployeeActive = E.[IsActive],
            @IsEmployeeDepartmentActive = D.[IsActive]
        FROM [HumanResources].[Employees] AS E WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN [HumanResources].[Departments] AS D
            ON D.[DepartmentId] = E.[DepartmentId]
        WHERE E.[EmployeeId] = @EmployeeId;

        IF @IsEmployeeActive IS NULL
        BEGIN
            THROW 56413,
                N'The employee was not found.',
                1;
        END;

        IF @IsEmployeeActive <> 1
           OR @IsEmployeeDepartmentActive <> 1
        BEGIN
            THROW 56414,
                N'The employee or department is inactive.',
                1;
        END;

        SELECT
            @DisabilityTypeCode = [DisabilityTypeCode],
            @DisabilityTypeName = [Name],
            @IsDisabilityTypeActive = [IsActive]
        FROM [Payroll].[DisabilityTypes] WITH (UPDLOCK, HOLDLOCK)
        WHERE [DisabilityTypeId] = @DisabilityTypeId;

        IF @DisabilityTypeCode IS NULL
        BEGIN
            THROW 56415,
                N'The disability type was not found.',
                1;
        END;

        IF @IsDisabilityTypeActive <> 1
        BEGIN
            THROW 56416,
                N'The disability type is inactive.',
                1;
        END;

        IF @NormalizedReferenceNumber IS NOT NULL
           AND EXISTS
           (
               SELECT 1
               FROM [Payroll].[EmployeeDisabilities] WITH (UPDLOCK, HOLDLOCK)
               WHERE [ReferenceNumber] = @NormalizedReferenceNumber
           )
        BEGIN
            THROW 56417,
                N'The disability reference number already exists.',
                1;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM [Payroll].[EmployeeDisabilities] WITH (UPDLOCK, HOLDLOCK)
            WHERE [EmployeeId] = @EmployeeId
              AND [DisabilityStatus] IN (N'Pending', N'Approved')
              AND [StartDate] <= @EndDate
              AND [EndDate] >= @StartDate
        )
        BEGIN
            THROW 56418,
                N'The employee already has an active disability record in the selected date range.',
                1;
        END;

        INSERT INTO [Payroll].[EmployeeDisabilities]
        (
            [EmployeeId],
            [DisabilityTypeId],
            [IssuerInstitution],
            [ReferenceNumber],
            [StartDate],
            [EndDate],
            [ReportedDate],
            [DisabilityStatus],
            [EmployerPaidAmount],
            [SubsidyAmount],
            [Notes],
            [CreatedAtUtc],
            [CreatedByUserId]
        )
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
        VALUES
        (
            @EmployeeId,
            @DisabilityTypeId,
            CONVERT(nvarchar(30), @NormalizedIssuerInstitution),
            CONVERT(nvarchar(100), @NormalizedReferenceNumber),
            @StartDate,
            @EndDate,
            @ReportedDate,
            N'Pending',
            @EmployerPaidAmount,
            @SubsidyAmount,
            @NormalizedNotes,
            @OccurredAtUtc,
            @ActorUserId
        );

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
            N'EmployeeDisabilityCreated',
            N'EmployeeDisabilities',
            CONVERT(nvarchar(100), R.[EmployeeDisabilityId]),
            N'User',
            @ActorUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            N'Employee disability record created successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'POST',
            @RequestPath,
            NULL,
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
