CREATE PROCEDURE [Payroll].[GetEmployeeDisabilityById]
    @EmployeeDisabilityId int,
    @ActorUserId int
AS
BEGIN
    SET NOCOUNT ON;

    IF @EmployeeDisabilityId IS NULL OR @EmployeeDisabilityId <= 0
    BEGIN
        THROW 57401,
            N'EmployeeDisabilityId must be greater than zero.',
            1;
    END;

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
    BEGIN
        THROW 57402,
            N'The ActorUserId must be greater than zero.',
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
        THROW 57403,
            N'The actor user was not found.',
            1;
    END;

    IF @IsActorUserActive = 0 OR @IsActorRoleActive = 0
    BEGIN
        THROW 57404,
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
        THROW 57405,
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
        THROW 57406,
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
    INNER JOIN [Payroll].[DisabilityTypes] AS DT
        ON DT.[DisabilityTypeId] = ED.[DisabilityTypeId]
    WHERE ED.[EmployeeDisabilityId] = @EmployeeDisabilityId;
END;
GO
