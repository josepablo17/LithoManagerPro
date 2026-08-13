SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @VacationLeaveTypeId int;

SELECT
    @VacationLeaveTypeId = LeaveTypeId
FROM LeaveManagement.LeaveTypes
WHERE LeaveTypeCode = N'Vacation';

IF @VacationLeaveTypeId IS NULL
BEGIN
    THROW 51000, 'Vacation leave type seed is required before leave policies.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM LeaveManagement.LeavePolicies
    WHERE
        LeavePolicyCode <> N'CostaRicaVacationStandard'
        AND LeaveTypeId = @VacationLeaveTypeId
        AND IsActive = 1
)
BEGIN
    THROW 51001, 'Only one active leave policy per leave type is allowed.', 1;
END;

UPDATE LeaveManagement.LeavePolicies
SET
    LeaveTypeId = @VacationLeaveTypeId,
    Name = N'Costa Rica Vacation Standard',
    EntitlementDays = 12.00,
    EntitlementWeeks = 50,
    UsesBusinessDays = 1,
    IsActive = 1,
    UpdatedAtUtc = SYSUTCDATETIME()
WHERE LeavePolicyCode = N'CostaRicaVacationStandard'
    AND
    (
        LeaveTypeId <> @VacationLeaveTypeId
        OR Name <> N'Costa Rica Vacation Standard'
        OR EntitlementDays <> 12.00
        OR EntitlementWeeks <> 50
        OR UsesBusinessDays <> 1
        OR IsActive <> 1
    );

INSERT INTO LeaveManagement.LeavePolicies
(
    LeaveTypeId,
    LeavePolicyCode,
    Name,
    EntitlementDays,
    EntitlementWeeks,
    UsesBusinessDays,
    IsActive
)
SELECT
    @VacationLeaveTypeId,
    N'CostaRicaVacationStandard',
    N'Costa Rica Vacation Standard',
    12.00,
    50,
    1,
    1
WHERE NOT EXISTS
(
    SELECT 1
    FROM LeaveManagement.LeavePolicies
    WHERE LeavePolicyCode = N'CostaRicaVacationStandard'
);
