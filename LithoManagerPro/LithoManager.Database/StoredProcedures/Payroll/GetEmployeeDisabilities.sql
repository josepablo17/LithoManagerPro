CREATE PROCEDURE [Payroll].[GetEmployeeDisabilities]
    @ActorUserId int,
    @EmployeeId int = NULL,
    @DepartmentId int = NULL,
    @DisabilityTypeId int = NULL,
    @DisabilityStatus nvarchar(4000) = NULL,
    @IssuerInstitution nvarchar(4000) = NULL,
    @DateFrom date = NULL,
    @DateTo date = NULL,
    @SearchTerm nvarchar(4000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NormalizedDisabilityStatus nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@DisabilityStatus)), N'');

    DECLARE @NormalizedIssuerInstitution nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@IssuerInstitution)), N'');

    DECLARE @NormalizedSearchTerm nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
    BEGIN
        THROW 57301,
            N'The ActorUserId must be greater than zero.',
            1;
    END;

    IF @EmployeeId IS NOT NULL AND @EmployeeId <= 0
    BEGIN
        THROW 57302,
            N'EmployeeId must be greater than zero.',
            1;
    END;

    IF @DepartmentId IS NOT NULL AND @DepartmentId <= 0
    BEGIN
        THROW 57303,
            N'DepartmentId must be greater than zero.',
            1;
    END;

    IF @DisabilityTypeId IS NOT NULL AND @DisabilityTypeId <= 0
    BEGIN
        THROW 57304,
            N'DisabilityTypeId must be greater than zero.',
            1;
    END;

    IF @NormalizedDisabilityStatus IS NOT NULL
       AND @NormalizedDisabilityStatus NOT IN
       (
           N'Pending',
           N'Approved',
           N'Cancelled'
       )
    BEGIN
        THROW 57305,
            N'DisabilityStatus is invalid.',
            1;
    END;

    IF @NormalizedIssuerInstitution IS NOT NULL
       AND @NormalizedIssuerInstitution NOT IN
       (
           N'CCSS',
           N'INS',
           N'Employer',
           N'Other'
       )
    BEGIN
        THROW 57306,
            N'IssuerInstitution is invalid.',
            1;
    END;

    IF @DateFrom IS NOT NULL
       AND @DateTo IS NOT NULL
       AND @DateTo < @DateFrom
    BEGIN
        THROW 57307,
            N'DateTo cannot be earlier than DateFrom.',
            1;
    END;

    IF @NormalizedSearchTerm IS NOT NULL
       AND LEN(@NormalizedSearchTerm) > 150
    BEGIN
        THROW 57308,
            N'SearchTerm cannot exceed 150 characters.',
            1;
    END;

    DECLARE @ActorRoleCode nvarchar(50);
    DECLARE @IsActorUserActive bit;
    DECLARE @IsActorRoleActive bit;
    DECLARE @ActorEmployeeId int;
    DECLARE @IsActorEmployeeActive bit;
    DECLARE @IsActorDepartmentActive bit;

    SELECT
        @ActorRoleCode = R.[RoleCode],
        @IsActorUserActive = U.[IsActive],
        @IsActorRoleActive = R.[IsActive],
        @ActorEmployeeId = E.[EmployeeId],
        @IsActorEmployeeActive = E.[IsActive],
        @IsActorDepartmentActive = D.[IsActive]
    FROM [Security].[Users] AS U
    INNER JOIN [Security].[Roles] AS R
        ON R.[RoleId] = U.[RoleId]
    LEFT JOIN [HumanResources].[Employees] AS E
        ON E.[UserId] = U.[UserId]
    LEFT JOIN [HumanResources].[Departments] AS D
        ON D.[DepartmentId] = E.[DepartmentId]
    WHERE U.[UserId] = @ActorUserId;

    IF @ActorRoleCode IS NULL
    BEGIN
        THROW 57309,
            N'The actor user was not found.',
            1;
    END;

    IF @IsActorUserActive = 0 OR @IsActorRoleActive = 0
    BEGIN
        THROW 57310,
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
        THROW 57311,
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
        THROW 57312,
            N'The actor role is not allowed to query disability records.',
            1;
    END;

    SELECT
        ED.[EmployeeDisabilityId],
        ED.[EmployeeId],
        E.[IdentificationType],
        E.[IdentificationNumber],
        E.[FirstName],
        E.[LastName],
        ED.[DisabilityTypeId],
        DT.[DisabilityTypeCode],
        DT.[Name] AS [DisabilityTypeName],
        DT.[CountsAsSalaryForAguinaldo],
        DT.[RequiresSubsidyTracking],
        DT.[ReducesWorkedDays],
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
        ED.[CancelledAtUtc],
        ED.[CancelledByUserId],
        ED.[CancellationReason],
        ED.[Notes],
        ED.[CreatedAtUtc],
        ED.[CreatedByUserId],
        ED.[UpdatedAtUtc],
        ED.[UpdatedByUserId],
        ED.[RowVersion]
    FROM [Payroll].[EmployeeDisabilities] AS ED
    INNER JOIN [HumanResources].[Employees] AS E
        ON E.[EmployeeId] = ED.[EmployeeId]
    INNER JOIN [HumanResources].[Departments] AS D
        ON D.[DepartmentId] = E.[DepartmentId]
    INNER JOIN [Payroll].[DisabilityTypes] AS DT
        ON DT.[DisabilityTypeId] = ED.[DisabilityTypeId]
    WHERE
        (@EmployeeId IS NULL OR ED.[EmployeeId] = @EmployeeId)
        AND
        (@DepartmentId IS NULL OR D.[DepartmentId] = @DepartmentId)
        AND
        (@DisabilityTypeId IS NULL OR ED.[DisabilityTypeId] = @DisabilityTypeId)
        AND
        (
            @NormalizedDisabilityStatus IS NULL
            OR ED.[DisabilityStatus] = @NormalizedDisabilityStatus
        )
        AND
        (
            @NormalizedIssuerInstitution IS NULL
            OR ED.[IssuerInstitution] = @NormalizedIssuerInstitution
        )
        AND
        (@DateFrom IS NULL OR ED.[EndDate] >= @DateFrom)
        AND
        (@DateTo IS NULL OR ED.[StartDate] <= @DateTo)
        AND
        (
            @NormalizedSearchTerm IS NULL
            OR E.[IdentificationNumber] LIKE N'%' + @NormalizedSearchTerm + N'%'
            OR E.[FirstName] LIKE N'%' + @NormalizedSearchTerm + N'%'
            OR E.[LastName] LIKE N'%' + @NormalizedSearchTerm + N'%'
            OR ED.[ReferenceNumber] LIKE N'%' + @NormalizedSearchTerm + N'%'
        )
    ORDER BY
        ED.[StartDate] DESC,
        E.[LastName],
        E.[FirstName],
        ED.[EmployeeDisabilityId] DESC;
END;
GO
