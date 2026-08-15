CREATE PROCEDURE [Documents].[GetEmployeeDocumentById]
    @EmployeeDocumentId int,
    @ActorUserId int
AS
BEGIN
    SET NOCOUNT ON;

    IF @EmployeeDocumentId IS NULL
       OR @EmployeeDocumentId <= 0
    BEGIN
        THROW 55301,
            N'EmployeeDocumentId must be greater than zero.',
            1;
    END;

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 55302,
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
        THROW 55303,
            N'The actor user was not found.',
            1;
    END;

    IF @IsActorUserActive = 0
    BEGIN
        THROW 55304,
            N'The actor user account is inactive.',
            1;
    END;

    IF @IsActorRoleActive = 0
    BEGIN
        THROW 55305,
            N'The actor role is inactive.',
            1;
    END;

    IF @ActorEmployeeId IS NOT NULL
       AND @IsActorEmployeeActive <> 1
    BEGIN
        THROW 55306,
            N'The actor employee record is inactive.',
            1;
    END;

    IF @ActorEmployeeId IS NOT NULL
       AND @IsActorDepartmentActive <> 1
    BEGIN
        THROW 55307,
            N'The actor department is inactive.',
            1;
    END;

    IF @ActorRoleCode IN
    (
        N'SuperAdministrator',
        N'HumanResourcesAdministrator',
        N'HumanResourcesStaff'
    )
    BEGIN
        SELECT
            ED.[EmployeeDocumentId],
            ER.[EmployeeRecordId],
            ER.[EmployeeId],
            E.[IdentificationNumber],
            E.[FirstName],
            E.[LastName],
            D.[DepartmentId],
            D.[DepartmentCode],
            D.[Name] AS [DepartmentName],
            ED.[DocumentTypeId],
            DT.[DocumentTypeCode],
            DT.[Name] AS [DocumentTypeName],
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
        INNER JOIN [Documents].[EmployeeRecords] AS ER
            ON ER.[EmployeeRecordId] = ED.[EmployeeRecordId]
        INNER JOIN [HumanResources].[Employees] AS E
            ON E.[EmployeeId] = ER.[EmployeeId]
        INNER JOIN [HumanResources].[Departments] AS D
            ON D.[DepartmentId] = E.[DepartmentId]
        INNER JOIN [Documents].[DocumentTypes] AS DT
            ON DT.[DocumentTypeId] = ED.[DocumentTypeId]
        WHERE ED.[EmployeeDocumentId] = @EmployeeDocumentId;

        RETURN;
    END;

    IF @ActorEmployeeId IS NULL
    BEGIN
        THROW 55308,
            N'The actor user is not linked to an employee.',
            1;
    END;

    SELECT
        ED.[EmployeeDocumentId],
        ER.[EmployeeRecordId],
        ER.[EmployeeId],
        E.[IdentificationNumber],
        E.[FirstName],
        E.[LastName],
        D.[DepartmentId],
        D.[DepartmentCode],
        D.[Name] AS [DepartmentName],
        ED.[DocumentTypeId],
        DT.[DocumentTypeCode],
        DT.[Name] AS [DocumentTypeName],
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
    INNER JOIN [Documents].[EmployeeRecords] AS ER
        ON ER.[EmployeeRecordId] = ED.[EmployeeRecordId]
    INNER JOIN [HumanResources].[Employees] AS E
        ON E.[EmployeeId] = ER.[EmployeeId]
    INNER JOIN [HumanResources].[Departments] AS D
        ON D.[DepartmentId] = E.[DepartmentId]
    INNER JOIN [Documents].[DocumentTypes] AS DT
        ON DT.[DocumentTypeId] = ED.[DocumentTypeId]
    WHERE ED.[EmployeeDocumentId] = @EmployeeDocumentId
        AND ER.[EmployeeId] = @ActorEmployeeId
        AND ED.[IsActive] = 1
        AND ED.[IsVisibleToEmployee] = 1;
END;
GO
