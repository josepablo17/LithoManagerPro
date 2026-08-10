using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .HumanResources.Employees.CreateEmployee;

public sealed class CreateEmployeeService
    : ICreateEmployeeService
{
    private readonly IEmployeeRepository
        _employeeRepository;

    public CreateEmployeeService(
        IEmployeeRepository employeeRepository)
    {
        ArgumentNullException.ThrowIfNull(
            employeeRepository);

        _employeeRepository =
            employeeRepository;
    }

    public async Task<EmployeeResult> CreateAsync(
        CreateEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (!EmployeeValidation.IsValidMutationRequest(
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
                command.ProfileImagePath))
        {
            return EmployeeResult.Failure(
                EmployeeErrorCode.InvalidRequest);
        }

        try
        {
            EmployeeData employee =
                await _employeeRepository.CreateEmployeeAsync(
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
