using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .HumanResources.Employees;

internal static class EmployeeValidation
{
    private const int MaximumIdentificationNumberLength = 30;
    private const int MaximumFirstNameLength = 100;
    private const int MaximumLastNameLength = 150;
    private const int MaximumPhoneNumberLength = 25;
    private const int MaximumJobTitleLength = 100;
    private const int MaximumProfileImagePathLength = 500;
    private const int MaximumSearchTermLength = 150;

    public static bool IsValidUserId(
        int? userId)
    {
        return userId is null or > 0;
    }

    public static bool IsValidDepartmentId(
        int departmentId)
    {
        return departmentId > 0;
    }

    public static bool IsValidIdentificationNumber(
        string? identificationNumber)
    {
        return IsValidRequiredText(
            identificationNumber,
            MaximumIdentificationNumberLength);
    }

    public static bool IsValidFirstName(
        string? firstName)
    {
        return IsValidRequiredText(
            firstName,
            MaximumFirstNameLength);
    }

    public static bool IsValidLastName(
        string? lastName)
    {
        return IsValidRequiredText(
            lastName,
            MaximumLastNameLength);
    }

    public static bool IsValidPhoneNumber(
        string? phoneNumber)
    {
        return IsValidOptionalText(
            phoneNumber,
            MaximumPhoneNumberLength);
    }

    public static bool IsValidEmploymentDates(
        DateTime? hireDate,
        DateTime? terminationDate)
    {
        return hireDate is not null
            && (terminationDate is null
                || terminationDate.Value.Date
                    >= hireDate.Value.Date);
    }

    public static bool IsValidJobTitle(
        string? jobTitle)
    {
        return IsValidRequiredText(
            jobTitle,
            MaximumJobTitleLength);
    }

    public static bool IsValidBaseSalary(
        decimal? baseSalary)
    {
        return baseSalary is >= 0;
    }

    public static bool IsValidProfileImagePath(
        string? profileImagePath)
    {
        return IsValidOptionalText(
            profileImagePath,
            MaximumProfileImagePathLength);
    }

    public static bool IsValidSearchTerm(
        string? searchTerm)
    {
        return IsValidOptionalText(
            searchTerm,
            MaximumSearchTermLength);
    }

    public static bool IsValidDepartmentFilter(
        int? departmentId)
    {
        return departmentId is null or > 0;
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

    private static bool IsValidRequiredText(
        string? value,
        int maximumLength)
    {
        string? normalizedValue =
            Normalize(value);

        return normalizedValue is not null
            && normalizedValue.Length <= maximumLength;
    }

    private static bool IsValidOptionalText(
        string? value,
        int maximumLength)
    {
        string? normalizedValue =
            Normalize(value);

        return normalizedValue is null
            || normalizedValue.Length <= maximumLength;
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
