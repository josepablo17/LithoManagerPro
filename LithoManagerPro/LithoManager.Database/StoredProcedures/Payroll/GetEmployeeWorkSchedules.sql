-- Lists employee work schedules. By default only the current schedule
-- of each employee (EffectiveToDate IS NULL); @IncludeHistory = 1 also
-- returns the closed ones. Same columns as SetEmployeeWorkSchedule.
CREATE PROCEDURE [Payroll].[GetEmployeeWorkSchedules]
    @EmployeeId int = NULL,
    @IncludeHistory bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        EWS.[EmployeeWorkScheduleId],
        EWS.[EmployeeId],
        E.[IdentificationType],
        E.[IdentificationNumber],
        E.[FirstName],
        E.[LastName],
        EWS.[WorkShiftTypeId],
        WST.[WorkShiftTypeCode],
        WST.[Name] AS [WorkShiftTypeName],
        EWS.[WeeklyOrdinaryHours],
        EWS.[WorksMonday],
        EWS.[WorksTuesday],
        EWS.[WorksWednesday],
        EWS.[WorksThursday],
        EWS.[WorksFriday],
        EWS.[WorksSaturday],
        EWS.[WorksSunday],
        EWS.[EffectiveFromDate],
        EWS.[EffectiveToDate],
        EWS.[IsActive],
        EWS.[CreatedAtUtc],
        EWS.[CreatedByUserId],
        EWS.[UpdatedAtUtc],
        EWS.[UpdatedByUserId],
        EWS.[RowVersion]
    FROM [Payroll].[EmployeeWorkSchedules] AS EWS
    INNER JOIN [HumanResources].[Employees] AS E
        ON E.[EmployeeId] = EWS.[EmployeeId]
    INNER JOIN [Payroll].[WorkShiftTypes] AS WST
        ON WST.[WorkShiftTypeId] = EWS.[WorkShiftTypeId]
    WHERE (@EmployeeId IS NULL OR EWS.[EmployeeId] = @EmployeeId)
      AND (ISNULL(@IncludeHistory, 0) = 1 OR EWS.[EffectiveToDate] IS NULL)
    ORDER BY
        E.[FirstName],
        E.[LastName],
        EWS.[EffectiveFromDate] DESC;
END;
GO
