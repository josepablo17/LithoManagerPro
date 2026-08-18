namespace LithoManager.Api.Authorization;

public static class AuthorizationPolicyNames
{
    public const string PasswordChangeOnly =
        "PasswordChangeOnly";

    public const string HumanResourcesDepartments =
        "HumanResourcesDepartments";

    public const string HumanResourcesEmployees =
        "HumanResourcesEmployees";

    public const string LeaveManagementAdministration =
        "LeaveManagementAdministration";

    public const string LeaveManagementAdministrationMutation =
        "LeaveManagementAdministrationMutation";

    public const string DocumentAdministration =
        "DocumentAdministration";

    public const string DocumentAdministrationMutation =
        "DocumentAdministrationMutation";

    public const string PayrollAdministration =
        "PayrollAdministration";

    public const string PayrollAdministrationMutation =
        "PayrollAdministrationMutation";
}
