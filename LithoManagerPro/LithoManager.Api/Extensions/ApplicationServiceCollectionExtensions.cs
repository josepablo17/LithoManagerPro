using LithoManager.Application.Abstractions.Security;
using LithoManager.Application.Features.Authentication
    .ChangePassword;
using LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;
using LithoManager.Application.Features.Authentication
    .ForgotPassword;
using LithoManager.Application.Features.Authentication
    .GetCurrentUser;
using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features.Authentication
    .ResetPassword;
using LithoManager.Application.Features
    .HumanResources.Departments.CreateDepartment;
using LithoManager.Application.Features
    .HumanResources.Departments.GetDepartmentById;
using LithoManager.Application.Features
    .HumanResources.Departments.GetDepartments;
using LithoManager.Application.Features
    .HumanResources.Departments.SetDepartmentStatus;
using LithoManager.Application.Features
    .HumanResources.Departments.UpdateDepartment;
using LithoManager.Application.Features
    .HumanResources.Employees.CreateEmployee;
using LithoManager.Application.Features
    .HumanResources.Employees.GetEmployeeById;
using LithoManager.Application.Features
    .HumanResources.Employees.GetEmployees;
using LithoManager.Application.Features
    .HumanResources.Employees.SetEmployeeStatus;
using LithoManager.Application.Features
    .HumanResources.Employees.UpdateEmployee;
using LithoManager.Application.Security;

namespace LithoManager.Api.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<
            IPasswordPolicy,
            PasswordPolicy>();

        services.AddScoped<
            IAuthenticationService,
            AuthenticationService>();

        services.AddScoped<
            IChangeTemporaryPasswordService,
            ChangeTemporaryPasswordService>();

        services.AddScoped<
            IChangePasswordService,
            ChangePasswordService>();

        services.AddScoped<
            IForgotPasswordService,
            ForgotPasswordService>();

        services.AddScoped<
            IResetPasswordService,
            ResetPasswordService>();

        services.AddScoped<
            IGetCurrentUserService,
            GetCurrentUserService>();

        services.AddScoped<
            ICreateDepartmentService,
            CreateDepartmentService>();

        services.AddScoped<
            IGetDepartmentByIdService,
            GetDepartmentByIdService>();

        services.AddScoped<
            IGetDepartmentsService,
            GetDepartmentsService>();

        services.AddScoped<
            IUpdateDepartmentService,
            UpdateDepartmentService>();

        services.AddScoped<
            ISetDepartmentStatusService,
            SetDepartmentStatusService>();

        services.AddScoped<
            ICreateEmployeeService,
            CreateEmployeeService>();

        services.AddScoped<
            IGetEmployeeByIdService,
            GetEmployeeByIdService>();

        services.AddScoped<
            IGetEmployeesService,
            GetEmployeesService>();

        services.AddScoped<
            IUpdateEmployeeService,
            UpdateEmployeeService>();

        services.AddScoped<
            ISetEmployeeStatusService,
            SetEmployeeStatusService>();

        return services;
    }
}
