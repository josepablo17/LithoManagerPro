using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features.LeaveManagement;

internal static class LeaveManagementValidation
{
    public const string DefaultVacationLeaveTypeCode =
        "Vacation";

    private const int MaximumLeaveTypeCodeLength = 50;
    private const int MaximumStatusCodeLength = 30;
    private const int MaximumSearchTermLength = 150;

    public static bool IsValidLeaveTypeCode(
        string? leaveTypeCode)
    {
        string? normalizedLeaveTypeCode =
            NormalizeLeaveTypeCode(leaveTypeCode);

        if (normalizedLeaveTypeCode is null
            || normalizedLeaveTypeCode.Length
                > MaximumLeaveTypeCodeLength)
        {
            return false;
        }

        return !normalizedLeaveTypeCode.Contains(
            ' ',
            StringComparison.Ordinal);
    }

    public static bool IsValidStatusCode(
        string? statusCode)
    {
        string? normalizedStatusCode =
            Normalize(statusCode);

        if (normalizedStatusCode is null)
        {
            return true;
        }

        if (normalizedStatusCode.Length
            > MaximumStatusCodeLength)
        {
            return false;
        }

        return !normalizedStatusCode.Contains(
            ' ',
            StringComparison.Ordinal);
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

    public static bool IsValidDateRange(
        DateTime? startDateFrom,
        DateTime? startDateTo)
    {
        return !startDateFrom.HasValue
            || !startDateTo.HasValue
            || startDateTo.Value.Date
                >= startDateFrom.Value.Date;
    }

    public static bool IsValidLeaveRequestDates(
        DateTime? startDate,
        DateTime? endDate)
    {
        return startDate is not null
            && endDate is not null
            && endDate.Value.Date
                >= startDate.Value.Date;
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

    public static string NormalizeLeaveTypeCode(
        string? leaveTypeCode)
    {
        return Normalize(leaveTypeCode)
            ?? DefaultVacationLeaveTypeCode;
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
