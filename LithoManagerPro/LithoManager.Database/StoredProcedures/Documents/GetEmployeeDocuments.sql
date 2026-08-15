CREATE PROCEDURE [Documents].[GetEmployeeDocuments]
    @ActorUserId int,
    @EmployeeId int = NULL,
    @DocumentTypeId int = NULL,
    @IsActive bit = 1,
    @IsVisibleToEmployee bit = NULL,
    @CreatedFromUtc datetime2(3) = NULL,
    @CreatedToUtc datetime2(3) = NULL,
    @SearchTerm nvarchar(4000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NormalizedSearchTerm nvarchar(4000) =
        NULLIF(
            LTRIM(RTRIM(@SearchTerm)),
            N''
        );

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 55201,
            N'The ActorUserId must be greater than zero.',
            1;
    END;

    IF @EmployeeId IS NOT NULL
       AND @EmployeeId <= 0
    BEGIN
        THROW 55202,
            N'EmployeeId must be greater than zero when provided.',
            1;
    END;

    IF @DocumentTypeId IS NOT NULL
       AND @DocumentTypeId <= 0
    BEGIN
        THROW 55203,
            N'DocumentTypeId must be greater than zero when provided.',
            1;
    END;

    IF @CreatedFromUtc IS NOT NULL
       AND @CreatedToUtc IS NOT NULL
       AND @CreatedToUtc < @CreatedFromUtc
    BEGIN
        THROW 55204,
            N'CreatedToUtc cannot be earlier than CreatedFromUtc.',
            1;
    END;

    IF @NormalizedSearchTerm IS NOT NULL
       AND LEN(@NormalizedSearchTerm) > 150
    BEGIN
        THROW 55205,
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
        THROW 55206,
            N'The actor user was not found.',
            1;
    END;

    IF @IsActorUserActive = 0
    BEGIN
        THROW 55207,
            N'The actor user account is inactive.',
            1;
    END;

    IF @IsActorRoleActive = 0
    BEGIN
        THROW 55208,
            N'The actor role is inactive.',
            1;
    END;

    IF @ActorEmployeeId IS NOT NULL
       AND @IsActorEmployeeActive <> 1
    BEGIN
        THROW 55209,
            N'The actor employee record is inactive.',
            1;
    END;

    IF @ActorEmployeeId IS NOT NULL
       AND @IsActorDepartmentActive <> 1
    BEGIN
        THROW 55210,
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
        IF @ActorEmployeeId IS NULL
        BEGIN
            THROW 55211,
                N'The actor user is not linked to an employee.',
                1;
        END;

        IF @EmployeeId IS NOT NULL
           AND @EmployeeId <> @ActorEmployeeId
        BEGIN
            THROW 55212,
                N'Employees can only list their own documents.',
                1;
        END;

        SET @EmployeeId = @ActorEmployeeId;
        SET @IsActive = 1;
        SET @IsVisibleToEmployee = 1;
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
    WHERE
        (
            @EmployeeId IS NULL
            OR ER.[EmployeeId] = @EmployeeId
        )
        AND
        (
            @DocumentTypeId IS NULL
            OR ED.[DocumentTypeId] = @DocumentTypeId
        )
        AND
        (
            @IsActive IS NULL
            OR ED.[IsActive] = @IsActive
        )
        AND
        (
            @IsVisibleToEmployee IS NULL
            OR ED.[IsVisibleToEmployee] = @IsVisibleToEmployee
        )
        AND
        (
            @CreatedFromUtc IS NULL
            OR ED.[CreatedAtUtc] >= @CreatedFromUtc
        )
        AND
        (
            @CreatedToUtc IS NULL
            OR ED.[CreatedAtUtc] <= @CreatedToUtc
        )
        AND
        (
            @NormalizedSearchTerm IS NULL
            OR ED.[Title] LIKE N'%' + @NormalizedSearchTerm + N'%'
            OR ED.[OriginalFileName] LIKE
                N'%' + @NormalizedSearchTerm + N'%'
            OR E.[IdentificationNumber] LIKE
                N'%' + @NormalizedSearchTerm + N'%'
            OR E.[FirstName] LIKE N'%' + @NormalizedSearchTerm + N'%'
            OR E.[LastName] LIKE N'%' + @NormalizedSearchTerm + N'%'
            OR DT.[Name] LIKE N'%' + @NormalizedSearchTerm + N'%'
        )
    ORDER BY
        ED.[CreatedAtUtc] DESC,
        ED.[EmployeeDocumentId] DESC;
END;
GO
