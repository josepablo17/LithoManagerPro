using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .Payroll.CreateEmployeeDisability;

public sealed record CreateEmployeeDisabilityCommand(
    int EmployeeId,
    int DisabilityTypeId,
    string? IssuerInstitution,
    DateTime? StartDate,
    DateTime? EndDate,
    string? ReferenceNumber,
    decimal? EmployerPaidAmount,
    decimal? SubsidyAmount,
    string? Notes,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
