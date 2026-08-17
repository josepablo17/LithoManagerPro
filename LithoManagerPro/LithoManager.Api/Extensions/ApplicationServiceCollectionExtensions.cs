using LithoManager.Application.Abstractions.Security;
using LithoManager.Application.Features.Authentication;
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
using LithoManager.Application.Features.Authentication.Logout;
using LithoManager.Application.Features.Authentication
    .RefreshSession;
using LithoManager.Application.Features.Authentication
    .ResetPassword;
using LithoManager.Application.Features
    .Documents.CreateEmployeeDocument;
using LithoManager.Application.Features
    .Documents.EnsureEmployeeRecord;
using LithoManager.Application.Features
    .Documents.GetDocumentTypes;
using LithoManager.Application.Features
    .Documents.GetEmployeeDocumentById;
using LithoManager.Application.Features
    .Documents.GetEmployeeDocumentDownloadContext;
using LithoManager.Application.Features
    .Documents.GetEmployeeDocuments;
using LithoManager.Application.Features
    .Documents.SetEmployeeDocumentStatus;
using LithoManager.Application.Features
    .Documents.UpdateEmployeeDocument;
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
    .HumanResources.Employees.GetAssignableEmployeeUsers;
using LithoManager.Application.Features
    .HumanResources.Employees.GetEmployeeById;
using LithoManager.Application.Features
    .HumanResources.Employees.GetEmployeeIdentificationTypes;
using LithoManager.Application.Features
    .HumanResources.Employees.GetEmployeeSalaryHistory;
using LithoManager.Application.Features
    .HumanResources.Employees.GetEmployees;
using LithoManager.Application.Features
    .HumanResources.Employees.SetEmployeeStatus;
using LithoManager.Application.Features
    .HumanResources.Employees.UpdateEmployee;
using LithoManager.Application.Features
    .LeaveManagement.AdjustEmployeeLeaveBalance;
using LithoManager.Application.Features
    .LeaveManagement.CancelLeaveRequest;
using LithoManager.Application.Features
    .LeaveManagement.CreateLeaveRequest;
using LithoManager.Application.Features
    .LeaveManagement.GetEmployeeLeaveBalance;
using LithoManager.Application.Features
    .LeaveManagement.GetLeaveRequestById;
using LithoManager.Application.Features
    .LeaveManagement.GetLeaveRequests;
using LithoManager.Application.Features
    .LeaveManagement.GetLeaveRequestStatuses;
using LithoManager.Application.Features
    .LeaveManagement.GetLeaveTypes;
using LithoManager.Application.Features
    .LeaveManagement.GetMyLeaveRequests;
using LithoManager.Application.Features
    .LeaveManagement.RespondLeaveRequest;
using LithoManager.Application.Security;
using Microsoft.Extensions.Configuration;

namespace LithoManager.Api.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        AuthenticationSessionOptions sessionOptions =
            configuration
                .GetSection(
                    AuthenticationSessionOptions.SectionName)
                .Get<AuthenticationSessionOptions>()
            ?? new AuthenticationSessionOptions();

        if (sessionOptions.RefreshTokenExpirationDays
            is < 1 or > 30)
        {
            throw new InvalidOperationException(
                "Authentication:Session:" +
                "RefreshTokenExpirationDays must be " +
                "between 1 and 30.");
        }

        services.AddSingleton(sessionOptions);

        AuthenticationSecurityOptions securityOptions =
            configuration
                .GetSection(
                    AuthenticationSecurityOptions.SectionName)
                .Get<AuthenticationSecurityOptions>()
            ?? new AuthenticationSecurityOptions();

        if (securityOptions
                .PasswordResetTokenExpirationMinutes
            is < 1 or > 1440)
        {
            throw new InvalidOperationException(
                "Authentication:Security:" +
                "PasswordResetTokenExpirationMinutes " +
                "must be between 1 and 1440.");
        }

        if (securityOptions.MaximumFailedLoginAttempts
            is < 1 or > 20)
        {
            throw new InvalidOperationException(
                "Authentication:Security:" +
                "MaximumFailedLoginAttempts must be " +
                "between 1 and 20.");
        }

        if (securityOptions.LockoutDurationMinutes
            is < 1 or > 1440)
        {
            throw new InvalidOperationException(
                "Authentication:Security:" +
                "LockoutDurationMinutes must be " +
                "between 1 and 1440.");
        }

        services.AddSingleton(securityOptions);

        services.AddSingleton<
            IPasswordPolicy,
            PasswordPolicy>();

        services.AddScoped<
            IAuthenticationService,
            AuthenticationService>();

        services.AddScoped<
            IRefreshSessionService,
            RefreshSessionService>();

        services.AddScoped<
            ILogoutService,
            LogoutService>();

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
            IGetAssignableEmployeeUsersService,
            GetAssignableEmployeeUsersService>();

        services.AddScoped<
            IGetEmployeeByIdService,
            GetEmployeeByIdService>();

        services.AddScoped<
            IGetEmployeeIdentificationTypesService,
            GetEmployeeIdentificationTypesService>();

        services.AddScoped<
            IGetEmployeesService,
            GetEmployeesService>();

        services.AddScoped<
            IGetEmployeeSalaryHistoryService,
            GetEmployeeSalaryHistoryService>();

        services.AddScoped<
            IUpdateEmployeeService,
            UpdateEmployeeService>();

        services.AddScoped<
            ISetEmployeeStatusService,
            SetEmployeeStatusService>();

        services.AddScoped<
            IGetLeaveTypesService,
            GetLeaveTypesService>();

        services.AddScoped<
            IGetLeaveRequestStatusesService,
            GetLeaveRequestStatusesService>();

        services.AddScoped<
            IGetEmployeeLeaveBalanceService,
            GetEmployeeLeaveBalanceService>();

        services.AddScoped<
            IAdjustEmployeeLeaveBalanceService,
            AdjustEmployeeLeaveBalanceService>();

        services.AddScoped<
            IGetMyLeaveRequestsService,
            GetMyLeaveRequestsService>();

        services.AddScoped<
            IGetLeaveRequestsService,
            GetLeaveRequestsService>();

        services.AddScoped<
            IGetLeaveRequestByIdService,
            GetLeaveRequestByIdService>();

        services.AddScoped<
            ICreateLeaveRequestService,
            CreateLeaveRequestService>();

        services.AddScoped<
            ICancelLeaveRequestService,
            CancelLeaveRequestService>();

        services.AddScoped<
            IRespondLeaveRequestService,
            RespondLeaveRequestService>();

        services.AddScoped<
            IGetDocumentTypesService,
            GetDocumentTypesService>();

        services.AddScoped<
            IEnsureEmployeeRecordService,
            EnsureEmployeeRecordService>();

        services.AddScoped<
            IGetEmployeeDocumentsService,
            GetEmployeeDocumentsService>();

        services.AddScoped<
            IGetEmployeeDocumentByIdService,
            GetEmployeeDocumentByIdService>();

        services.AddScoped<
            IGetEmployeeDocumentDownloadContextService,
            GetEmployeeDocumentDownloadContextService>();

        services.AddScoped<
            ICreateEmployeeDocumentService,
            CreateEmployeeDocumentService>();

        services.AddScoped<
            IUpdateEmployeeDocumentService,
            UpdateEmployeeDocumentService>();

        services.AddScoped<
            ISetEmployeeDocumentStatusService,
            SetEmployeeDocumentStatusService>();

        return services;
    }
}
