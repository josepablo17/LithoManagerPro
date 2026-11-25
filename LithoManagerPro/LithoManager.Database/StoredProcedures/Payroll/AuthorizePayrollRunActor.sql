-- Shared actor validation for payroll run mutations. Each caller passes
-- the base of its own error range (for example 58100) so the error
-- numbers stay unique per procedure:
--   base + 5  actor id is missing or invalid
--   base + 6  actor user not found
--   base + 7  actor user or role inactive
--   base + 8  actor employee or department inactive
--   base + 9  actor role not allowed to manage payroll runs
CREATE PROCEDURE [Payroll].[AuthorizePayrollRunActor]
    @ActorUserId int,
    @ErrorNumberBase int,
    @ActorEmailAddress nvarchar(254) OUTPUT,
    @ActorRoleCode nvarchar(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ErrorNumber int;
    DECLARE @IsActorUserActive bit;
    DECLARE @IsActorRoleActive bit;
    DECLARE @ActorEmployeeId int;
    DECLARE @IsActorEmployeeActive bit;
    DECLARE @IsActorDepartmentActive bit;

    SET @ActorEmailAddress = NULL;
    SET @ActorRoleCode = NULL;

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
    BEGIN
        SET @ErrorNumber = @ErrorNumberBase + 5;

        THROW @ErrorNumber,
            N'The ActorUserId must be greater than zero.',
            1;
    END;

    SELECT
        @ActorEmailAddress = U.[EmailAddress],
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

    IF @ActorEmailAddress IS NULL
    BEGIN
        SET @ErrorNumber = @ErrorNumberBase + 6;

        THROW @ErrorNumber,
            N'The actor user was not found.',
            1;
    END;

    IF @IsActorUserActive = 0 OR @IsActorRoleActive = 0
    BEGIN
        SET @ErrorNumber = @ErrorNumberBase + 7;

        THROW @ErrorNumber,
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
        SET @ErrorNumber = @ErrorNumberBase + 8;

        THROW @ErrorNumber,
            N'The actor employee or department is inactive.',
            1;
    END;

    IF @ActorRoleCode NOT IN
    (
        N'SuperAdministrator',
        N'HumanResourcesAdministrator'
    )
    BEGIN
        SET @ErrorNumber = @ErrorNumberBase + 9;

        THROW @ErrorNumber,
            N'The actor role is not allowed to manage payroll runs.',
            1;
    END;
END;
GO
