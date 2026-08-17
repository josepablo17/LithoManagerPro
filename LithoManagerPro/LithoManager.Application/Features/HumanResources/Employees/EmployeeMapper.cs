namespace LithoManager.Application.Features
    .HumanResources.Employees;

internal static class EmployeeMapper
{
    public static AssignableEmployeeUserInfo Map(
        AssignableEmployeeUserData user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new AssignableEmployeeUserInfo(
            UserId:
                user.UserId,
            EmailAddress:
                user.EmailAddress,
            RoleId:
                user.RoleId,
            RoleCode:
                user.RoleCode,
            RoleName:
                user.RoleName,
            AssignedEmployeeId:
                user.AssignedEmployeeId,
            AssignedEmployeeFirstName:
                user.AssignedEmployeeFirstName,
            AssignedEmployeeLastName:
                user.AssignedEmployeeLastName);
    }

    public static EmployeeIdentificationTypeInfo Map(
        EmployeeIdentificationTypeData identificationType)
    {
        ArgumentNullException.ThrowIfNull(
            identificationType);

        return new EmployeeIdentificationTypeInfo(
            IdentificationType:
                identificationType.IdentificationType,
            Name:
                identificationType.Name,
            MinLength:
                identificationType.MinLength,
            MaxLength:
                identificationType.MaxLength,
            IsNumericOnly:
                identificationType.IsNumericOnly,
            AllowsLeadingZero:
                identificationType.AllowsLeadingZero,
            SortOrder:
                identificationType.SortOrder);
    }

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
            IdentificationType:
                employee.IdentificationType,
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

    public static EmployeeSalaryHistoryInfo Map(
        EmployeeSalaryHistoryData salaryHistory)
    {
        ArgumentNullException.ThrowIfNull(salaryHistory);

        return new EmployeeSalaryHistoryInfo(
            EmployeeSalaryHistoryId:
                salaryHistory.EmployeeSalaryHistoryId,
            EmployeeId:
                salaryHistory.EmployeeId,
            IdentificationType:
                salaryHistory.IdentificationType,
            IdentificationNumber:
                salaryHistory.IdentificationNumber,
            FirstName:
                salaryHistory.FirstName,
            LastName:
                salaryHistory.LastName,
            DepartmentId:
                salaryHistory.DepartmentId,
            DepartmentCode:
                salaryHistory.DepartmentCode,
            DepartmentName:
                salaryHistory.DepartmentName,
            BaseSalary:
                salaryHistory.BaseSalary,
            EffectiveFromDate:
                salaryHistory.EffectiveFromDate,
            EffectiveToDate:
                salaryHistory.EffectiveToDate,
            IsCurrent:
                salaryHistory.IsCurrent,
            CreatedAtUtc:
                salaryHistory.CreatedAtUtc,
            CreatedByUserId:
                salaryHistory.CreatedByUserId,
            UpdatedAtUtc:
                salaryHistory.UpdatedAtUtc,
            UpdatedByUserId:
                salaryHistory.UpdatedByUserId,
            RowVersion:
                salaryHistory.RowVersion);
    }
}
