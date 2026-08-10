using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .HumanResources.Departments;

internal static class DepartmentValidation
{
    private const int MaximumDepartmentCodeLength = 50;
    private const int MaximumNameLength = 100;
    private const int MaximumDescriptionLength = 300;
    private const int MaximumSearchTermLength = 100;

    public static bool IsValidDepartmentCode(
        string? departmentCode)
    {
        string? normalizedDepartmentCode =
            Normalize(departmentCode);

        if (normalizedDepartmentCode is null
            || normalizedDepartmentCode.Length
                > MaximumDepartmentCodeLength)
        {
            return false;
        }

        return !normalizedDepartmentCode.Contains(
            ' ',
            StringComparison.Ordinal);
    }

    public static bool IsValidName(
        string? name)
    {
        string? normalizedName =
            Normalize(name);

        return normalizedName is not null
            && normalizedName.Length <= MaximumNameLength;
    }

    public static bool IsValidDescription(
        string? description)
    {
        string? normalizedDescription =
            Normalize(description);

        return normalizedDescription is null
            || normalizedDescription.Length
                <= MaximumDescriptionLength;
    }

    public static bool IsValidSearchTerm(
        string? searchTerm)
    {
        string? normalizedSearchTerm =
            Normalize(searchTerm);

        return normalizedSearchTerm is null
            || normalizedSearchTerm.Length
                <= MaximumSearchTermLength;
    }

    public static bool IsValidRowVersion(
        byte[]? rowVersion)
    {
        return rowVersion is { Length: 8 };
    }

    public static bool IsValidMutationRequest(
        int actorUserId,
        AuthenticationRequestContext requestContext)
    {
        ArgumentNullException.ThrowIfNull(
            requestContext);

        return actorUserId > 0
            && requestContext.CorrelationId != Guid.Empty;
    }

    private static string? Normalize(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
