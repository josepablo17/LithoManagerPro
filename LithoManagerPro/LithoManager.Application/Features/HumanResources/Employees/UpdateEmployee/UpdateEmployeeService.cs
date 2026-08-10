using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .HumanResources.Employees.UpdateEmployee;

public sealed class UpdateEmployeeService
    : IUpdateEmployeeService
{
    private readonly IEmployeeRepository
        _employeeRepository;

    public UpdateEmployeeService(
        IEmployeeRepository employeeRepository)
    {
        ArgumentNullException.ThrowIfNull(
            employeeRepository);

        _employeeRepository =
            employeeRepository;
    }

    public async Task<EmployeeResult> UpdateAsync(
        UpdateEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (command.EmployeeId <= 0
            || !EmployeeValidation.IsValidMutationRequest(
                command.ActorUserId,
                command.RequestContext)
            || !EmployeeValidation.IsValidUserId(
                command.UserId)
            || !EmployeeValidation.IsValidDepartmentId(
                command.DepartmentId)
            || !EmployeeValidation
                .IsValidIdentificationNumber(
                    command.IdentificationNumber)
            || !EmployeeValidation.IsValidFirstName(
                command.FirstName)
            || !EmployeeValidation.IsValidLastName(
                command.LastName)
            || !EmployeeValidation.IsValidPhoneNumber(
                command.PhoneNumber)
            || !EmployeeValidation.IsValidEmploymentDates(
                command.HireDate,
                command.TerminationDate)
            || !EmployeeValidation.IsValidJobTitle(
                command.JobTitle)
            || !EmployeeValidation.IsValidBaseSalary(
                command.BaseSalary)
            || !EmployeeValidation.IsValidProfileImagePath(
                command.ProfileImagePath)
            || !EmployeeValidation.IsValidRowVersion(
                command.ExpectedRowVersion))
        {
            return EmployeeResult.Failure(
                EmployeeErrorCode.InvalidRequest);
        }

        try
        {
            EmployeeData employee =
                await _employeeRepository.UpdateEmployeeAsync(
                    employeeId:
                        command.EmployeeId,
                    userId:
                        command.UserId,
                    departmentId:
                        command.DepartmentId,
                    identificationNumber:
                        command.IdentificationNumber!,
                    firstName:
                        command.FirstName!,
                    lastName:
                        command.LastName!,
                    phoneNumber:
                        command.PhoneNumber,
                    birthDate:
                        command.BirthDate,
                    hireDate:
                        command.HireDate!.Value,
                    terminationDate:
                        command.TerminationDate,
                    jobTitle:
                        command.JobTitle!,
                    baseSalary:
                        command.BaseSalary!.Value,
                    profileImagePath:
                        command.ProfileImagePath,
                    expectedRowVersion:
                        command.ExpectedRowVersion!,
                    actorUserId:
                        command.ActorUserId,
                    requestContext:
                        command.RequestContext,
                    cancellationToken:
                        cancellationToken);

            return EmployeeResult.Success(
                EmployeeMapper.Map(employee));
        }
        catch (EmployeePersistenceException exception)
        {
            return EmployeeResult.Failure(
                exception.ErrorCode);
        }
    }
}
