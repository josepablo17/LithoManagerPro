namespace LithoManager.Application.Features
    .HumanResources.Employees.GetEmployeeSalaryHistory;

public sealed record GetEmployeeSalaryHistoryQuery(
    int ActorUserId,
    int EmployeeId,
    DateTime? EffectiveFromDate,
    DateTime? EffectiveToDate);
