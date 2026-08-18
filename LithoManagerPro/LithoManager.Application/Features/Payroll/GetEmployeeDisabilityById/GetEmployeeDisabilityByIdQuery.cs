namespace LithoManager.Application.Features
    .Payroll.GetEmployeeDisabilityById;

public sealed record GetEmployeeDisabilityByIdQuery(
    int EmployeeDisabilityId,
    int ActorUserId);
