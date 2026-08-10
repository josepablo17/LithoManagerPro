namespace LithoManager.Application.Features
    .HumanResources.Employees;

internal static class EmployeeMapper
{
    public static EmployeeInfo Map(
        EmployeeData employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        return new EmployeeInfo(
            EmployeeId:
                employee.EmployeeId,
            UserId:
                employee.UserId,
            EmailAddress:
                employee.EmailAddress,
            DepartmentId:
                employee.DepartmentId,
            DepartmentCode:
                employee.DepartmentCode,
            DepartmentName:
                employee.DepartmentName,
            IsDepartmentActive:
                employee.IsDepartmentActive,
            IdentificationNumber:
                employee.IdentificationNumber,
            FirstName:
                employee.FirstName,
            LastName:
                employee.LastName,
            PhoneNumber:
                employee.PhoneNumber,
            BirthDate:
                employee.BirthDate,
            HireDate:
                employee.HireDate,
            TerminationDate:
                employee.TerminationDate,
            JobTitle:
                employee.JobTitle,
            BaseSalary:
                employee.BaseSalary,
            ProfileImagePath:
                employee.ProfileImagePath,
            IsActive:
                employee.IsActive,
            CreatedAtUtc:
                employee.CreatedAtUtc,
            CreatedByUserId:
                employee.CreatedByUserId,
            UpdatedAtUtc:
                employee.UpdatedAtUtc,
            UpdatedByUserId:
                employee.UpdatedByUserId,
            RowVersion:
                employee.RowVersion);
    }
}
