namespace LithoManager.Application.Features.Authentication.Login;

public sealed class UserTokenValidationData
{
    public int UserId { get; init; }

    public int TokenVersion { get; init; }

    public bool IsUserActive { get; init; }

    public bool IsRoleActive { get; init; }

    public int? EmployeeId { get; init; }

    public bool? IsEmployeeActive { get; init; }
}
