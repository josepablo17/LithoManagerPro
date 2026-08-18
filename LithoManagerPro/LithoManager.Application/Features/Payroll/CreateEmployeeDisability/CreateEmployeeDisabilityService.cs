using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Payroll.CreateEmployeeDisability;

public sealed class CreateEmployeeDisabilityService
    : ICreateEmployeeDisabilityService
{
    private readonly IPayrollRepository _payrollRepository;

    public CreateEmployeeDisabilityService(
        IPayrollRepository payrollRepository)
    {
        ArgumentNullException.ThrowIfNull(payrollRepository);

        _payrollRepository = payrollRepository;
    }

    public async Task<EmployeeDisabilityResult> CreateAsync(
        CreateEmployeeDisabilityCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (!PayrollValidation.IsValidPositiveId(
                command.EmployeeId)
            || !PayrollValidation.IsValidPositiveId(
                command.DisabilityTypeId)
            || !PayrollValidation.IsValidIssuerInstitution(
                command.IssuerInstitution)
            || !PayrollValidation.IsValidDateRange(
                command.StartDate,
                command.EndDate)
            || !PayrollValidation
                .IsValidOptionalReferenceNumber(
                    command.ReferenceNumber)
            || !PayrollValidation.IsValidOptionalAmount(
                command.EmployerPaidAmount)
            || !PayrollValidation.IsValidOptionalAmount(
                command.SubsidyAmount)
            || !PayrollValidation.IsValidNotes(
                command.Notes)
            || !PayrollValidation.IsValidMutationRequest(
                command.ActorUserId,
                command.RequestContext))
        {
            return EmployeeDisabilityResult.Failure(
                PayrollErrorCode.InvalidRequest);
        }

        try
        {
            EmployeeDisabilityData employeeDisability =
                await _payrollRepository
                    .CreateEmployeeDisabilityAsync(
                        command.EmployeeId,
                        command.DisabilityTypeId,
                        PayrollValidation.NormalizeRequiredText(
                            command.IssuerInstitution!),
                        command.StartDate!.Value.Date,
                        command.EndDate!.Value.Date,
                        PayrollValidation.NormalizeOptionalText(
                            command.ReferenceNumber),
                        command.EmployerPaidAmount,
                        command.SubsidyAmount,
                        PayrollValidation.NormalizeOptionalText(
                            command.Notes),
                        command.ActorUserId,
                        command.RequestContext,
                        cancellationToken);

            return EmployeeDisabilityResult.Success(
                PayrollMapper.Map(employeeDisability));
        }
        catch (PayrollPersistenceException exception)
        {
            return EmployeeDisabilityResult.Failure(
                exception.ErrorCode);
        }
    }
}
