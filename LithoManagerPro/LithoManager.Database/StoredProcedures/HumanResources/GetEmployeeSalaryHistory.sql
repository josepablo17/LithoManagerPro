CREATE PROCEDURE [HumanResources].[GetEmployeeSalaryHistory]
    @ActorUserId int,
    @EmployeeId int,
    @EffectiveFromDate date = NULL,
    @EffectiveToDate date = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 52201,
            N'The ActorUserId must be greater than zero.',
            1;
    END;

    IF @EmployeeId IS NULL
       OR @EmployeeId <= 0
    BEGIN
        THROW 52202,
            N'EmployeeId must be greater than zero.',
            1;
    END;

    IF @EffectiveFromDate IS NOT NULL
       AND @EffectiveToDate IS NOT NULL
       AND @EffectiveToDate < @EffectiveFromDate
    BEGIN
        THROW 52203,
            N'EffectiveToDate cannot be earlier than EffectiveFromDate.',
            1;
    END;

    DECLARE @ActorRoleCode nvarchar(50);
    DECLARE @IsActorUserActive bit;
    DECLARE @IsActorRoleActive bit;
    DECLARE @ActorEmployeeId int;
    DECLARE @IsActorEmployeeActive bit;
    DECLARE @IsActorDepartmentActive bit;

    SELECT
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
    INNER JOIN [Security].[Roles] AS R
        ON R.[RoleId] = U.[RoleId]
    LEFT JOIN [HumanResources].[Employees] AS E
        ON E.[UserId] = U.[UserId]
    LEFT JOIN [HumanResources].[Departments] AS D
        ON D.[DepartmentId] = E.[DepartmentId]
    WHERE U.[UserId] = @ActorUserId;

    IF @ActorRoleCode IS NULL
    BEGIN
        THROW 52204,
            N'The actor user was not found.',
            1;
    END;

    IF @IsActorUserActive = 0
    BEGIN
        THROW 52205,
            N'The actor user account is inactive.',
            1;
    END;

    IF @IsActorRoleActive = 0
    BEGIN
        THROW 52206,
            N'The actor role is inactive.',
            1;
    END;

    IF @ActorEmployeeId IS NOT NULL
       AND @IsActorEmployeeActive <> 1
    BEGIN
        THROW 52207,
            N'The actor employee record is inactive.',
            1;
    END;

    IF @ActorEmployeeId IS NOT NULL
       AND @IsActorDepartmentActive <> 1
    BEGIN
        THROW 52208,
            N'The actor department is inactive.',
            1;
    END;

    IF @ActorRoleCode NOT IN
    (
        N'SuperAdministrator',
        N'HumanResourcesAdministrator'
    )
    BEGIN
        THROW 52209,
            N'The actor role is not allowed to list employee salary history.',
            1;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [HumanResources].[Employees] AS E
        WHERE E.[EmployeeId] = @EmployeeId
    )
    BEGIN
        THROW 52210,
            N'The employee was not found.',
            1;
    END;

    SELECT
        ESH.[EmployeeSalaryHistoryId],
        ESH.[EmployeeId],
        E.[IdentificationType],
        E.[IdentificationNumber],
        E.[FirstName],
        E.[LastName],
        E.[DepartmentId],
        D.[DepartmentCode],
        D.[Name] AS [DepartmentName],
        ESH.[BaseSalary],
        ESH.[EffectiveFromDate],
        ESH.[EffectiveToDate],
        CONVERT(
            bit,
            CASE
                WHEN ESH.[EffectiveToDate] IS NULL
                    THEN 1
                ELSE 0
            END
        ) AS [IsCurrent],
        ESH.[CreatedAtUtc],
        ESH.[CreatedByUserId],
        ESH.[UpdatedAtUtc],
        ESH.[UpdatedByUserId],
        ESH.[RowVersion]
    FROM [HumanResources].[EmployeeSalaryHistory] AS ESH
    INNER JOIN [HumanResources].[Employees] AS E
        ON E.[EmployeeId] = ESH.[EmployeeId]
    INNER JOIN [HumanResources].[Departments] AS D
        ON D.[DepartmentId] = E.[DepartmentId]
    WHERE ESH.[EmployeeId] = @EmployeeId
      AND
      (
          @EffectiveFromDate IS NULL
          OR ESH.[EffectiveToDate] IS NULL
          OR ESH.[EffectiveToDate] >= @EffectiveFromDate
      )
      AND
      (
          @EffectiveToDate IS NULL
          OR ESH.[EffectiveFromDate] <= @EffectiveToDate
      )
    ORDER BY
        ESH.[EffectiveFromDate] DESC,
        ESH.[EmployeeSalaryHistoryId] DESC;
END;
GO
