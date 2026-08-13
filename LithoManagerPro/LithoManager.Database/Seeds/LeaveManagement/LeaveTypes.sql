SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @LeaveTypes TABLE
(
    LeaveTypeCode nvarchar(50) NOT NULL PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    AffectsVacationBalance bit NOT NULL,
    IsActive bit NOT NULL
);

INSERT INTO @LeaveTypes
(
    LeaveTypeCode,
    Name,
    AffectsVacationBalance,
    IsActive
)
VALUES
(
    N'Vacation',
    N'Vacation',
    1,
    1
);

UPDATE TargetLeaveType
SET
    TargetLeaveType.Name = SourceLeaveType.Name,
    TargetLeaveType.AffectsVacationBalance = SourceLeaveType.AffectsVacationBalance,
    TargetLeaveType.IsActive = SourceLeaveType.IsActive,
    TargetLeaveType.UpdatedAtUtc = SYSUTCDATETIME()
FROM LeaveManagement.LeaveTypes AS TargetLeaveType
INNER JOIN @LeaveTypes AS SourceLeaveType
    ON SourceLeaveType.LeaveTypeCode = TargetLeaveType.LeaveTypeCode
WHERE
    TargetLeaveType.Name <> SourceLeaveType.Name
    OR TargetLeaveType.AffectsVacationBalance <> SourceLeaveType.AffectsVacationBalance
    OR TargetLeaveType.IsActive <> SourceLeaveType.IsActive;

INSERT INTO LeaveManagement.LeaveTypes
(
    LeaveTypeCode,
    Name,
    AffectsVacationBalance,
    IsActive
)
SELECT
    SourceLeaveType.LeaveTypeCode,
    SourceLeaveType.Name,
    SourceLeaveType.AffectsVacationBalance,
    SourceLeaveType.IsActive
FROM @LeaveTypes AS SourceLeaveType
WHERE NOT EXISTS
(
    SELECT 1
    FROM LeaveManagement.LeaveTypes AS ExistingLeaveType
    WHERE ExistingLeaveType.LeaveTypeCode = SourceLeaveType.LeaveTypeCode
);
