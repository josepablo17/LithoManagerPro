SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Statuses TABLE
(
    LeaveRequestStatusCode nvarchar(30) NOT NULL PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    SortOrder smallint NOT NULL,
    IsTerminal bit NOT NULL,
    IsActive bit NOT NULL
);

INSERT INTO @Statuses
(
    LeaveRequestStatusCode,
    Name,
    SortOrder,
    IsTerminal,
    IsActive
)
VALUES
(
    N'Pending',
    N'Pending',
    1,
    0,
    1
),
(
    N'Approved',
    N'Approved',
    2,
    1,
    1
),
(
    N'Rejected',
    N'Rejected',
    3,
    1,
    1
),
(
    N'Cancelled',
    N'Cancelled',
    4,
    1,
    1
);

UPDATE TargetStatus
SET
    TargetStatus.Name = SourceStatus.Name,
    TargetStatus.SortOrder = SourceStatus.SortOrder,
    TargetStatus.IsTerminal = SourceStatus.IsTerminal,
    TargetStatus.IsActive = SourceStatus.IsActive,
    TargetStatus.UpdatedAtUtc = SYSUTCDATETIME()
FROM LeaveManagement.LeaveRequestStatuses AS TargetStatus
INNER JOIN @Statuses AS SourceStatus
    ON SourceStatus.LeaveRequestStatusCode = TargetStatus.LeaveRequestStatusCode
WHERE
    TargetStatus.Name <> SourceStatus.Name
    OR TargetStatus.SortOrder <> SourceStatus.SortOrder
    OR TargetStatus.IsTerminal <> SourceStatus.IsTerminal
    OR TargetStatus.IsActive <> SourceStatus.IsActive;

INSERT INTO LeaveManagement.LeaveRequestStatuses
(
    LeaveRequestStatusCode,
    Name,
    SortOrder,
    IsTerminal,
    IsActive
)
SELECT
    SourceStatus.LeaveRequestStatusCode,
    SourceStatus.Name,
    SourceStatus.SortOrder,
    SourceStatus.IsTerminal,
    SourceStatus.IsActive
FROM @Statuses AS SourceStatus
WHERE NOT EXISTS
(
    SELECT 1
    FROM LeaveManagement.LeaveRequestStatuses AS ExistingStatus
    WHERE ExistingStatus.LeaveRequestStatusCode = SourceStatus.LeaveRequestStatusCode
);
