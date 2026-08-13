CREATE PROCEDURE [LeaveManagement].[GetLeaveRequestStatuses]
    @IsActive bit = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        LRS.[LeaveRequestStatusCode],
        LRS.[Name],
        LRS.[SortOrder],
        LRS.[IsTerminal],
        LRS.[IsActive],
        LRS.[CreatedAtUtc],
        LRS.[UpdatedAtUtc],
        LRS.[RowVersion]
    FROM [LeaveManagement].[LeaveRequestStatuses] AS LRS
    WHERE
        @IsActive IS NULL
        OR LRS.[IsActive] = @IsActive
    ORDER BY
        LRS.[SortOrder],
        LRS.[LeaveRequestStatusCode];
END;
GO
