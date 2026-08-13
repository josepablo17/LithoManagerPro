CREATE PROCEDURE [LeaveManagement].[GetLeaveTypes]
    @IsActive bit = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        LT.[LeaveTypeId],
        LT.[LeaveTypeCode],
        LT.[Name],
        LT.[AffectsVacationBalance],
        LT.[IsActive],
        LT.[CreatedAtUtc],
        LT.[CreatedByUserId],
        LT.[UpdatedAtUtc],
        LT.[UpdatedByUserId],
        LT.[RowVersion]
    FROM [LeaveManagement].[LeaveTypes] AS LT
    WHERE
        @IsActive IS NULL
        OR LT.[IsActive] = @IsActive
    ORDER BY
        LT.[Name],
        LT.[LeaveTypeId];
END;
GO
