namespace LithoManager.Application.Features
    .Payroll.GetOvertimeRecords;

public sealed record GetOvertimeRecordsQuery(
    int ActorUserId,
    int? EmployeeId,
    int? DepartmentId,
    int? OvertimeRuleId,
    string? ApprovalStatus,
    DateTime? DateFrom,
    DateTime? DateTo,
    string? SearchTerm);
