using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features.Payroll;

internal static class PayrollValidation
{
    public const string DefaultPeriodicity = "Monthly";

    private const int MaximumCodeLength = 50;
    private const int MaximumStatusLength = 30;
    private const int MaximumNotesLength = 500;
    private const int MaximumReasonLength = 300;
    private const int MaximumReferenceNumberLength = 100;
    private const int MaximumSearchTermLength = 150;

    public static bool IsValidPositiveId(int value)
    {
        return value > 0;
    }

    public static bool IsValidOptionalPositiveId(int? value)
    {
        return value is null or > 0;
    }

    public static bool IsValidTaxYear(int taxYear)
    {
        return taxYear is >= 2000 and <= 2100;
    }

    public static bool IsValidPeriodicity(string? periodicity)
    {
        string? normalizedPeriodicity = Normalize(periodicity);

        return normalizedPeriodicity is null
            || normalizedPeriodicity is "Monthly" or "Annual";
    }

    public static bool IsValidEffectiveDate(DateTime? value)
    {
        return value is not null;
    }

    public static bool IsValidDateRange(
        DateTime? startDate,
        DateTime? endDate)
    {
        return startDate is not null
            && endDate is not null
            && endDate.Value.Date >= startDate.Value.Date;
    }

    public static bool IsValidOptionalDateRange(
        DateTime? dateFrom,
        DateTime? dateTo)
    {
        return !dateFrom.HasValue
            || !dateTo.HasValue
            || dateTo.Value.Date >= dateFrom.Value.Date;
    }

    public static bool IsValidHours(decimal? hours)
    {
        return hours is >= 0 and <= 24;
    }

    public static bool IsValidPositiveHours(decimal? hours)
    {
        return hours is > 0 and <= 24;
    }

    public static bool IsValidAttendanceStatus(
        string? attendanceStatus)
    {
        string? normalizedAttendanceStatus =
            Normalize(attendanceStatus);

        return normalizedAttendanceStatus is
            "Present"
            or "Partial"
            or "Absent"
            or "Holiday"
            or "Leave"
            or "Disability";
    }

    public static bool IsValidOptionalAttendanceStatus(
        string? attendanceStatus)
    {
        return Normalize(attendanceStatus) is null
            || IsValidAttendanceStatus(attendanceStatus);
    }

    public static bool IsValidOptionalOvertimeApprovalStatus(
        string? approvalStatus)
    {
        string? normalizedApprovalStatus =
            Normalize(approvalStatus);

        return normalizedApprovalStatus is null
            || normalizedApprovalStatus is
                "Pending"
                or "Approved"
                or "Rejected"
                or "Cancelled";
    }

    public static bool IsValidOptionalDisabilityStatus(
        string? disabilityStatus)
    {
        string? normalizedDisabilityStatus =
            Normalize(disabilityStatus);

        return normalizedDisabilityStatus is null
            || normalizedDisabilityStatus is
                "Pending"
                or "Approved"
                or "Cancelled";
    }

    public static bool IsValidIssuerInstitution(
        string? issuerInstitution)
    {
        string? normalizedIssuerInstitution =
            Normalize(issuerInstitution);

        return normalizedIssuerInstitution is
            "CCSS"
            or "INS"
            or "Employer"
            or "Other";
    }

    public static bool IsValidOptionalIssuerInstitution(
        string? issuerInstitution)
    {
        return Normalize(issuerInstitution) is null
            || IsValidIssuerInstitution(issuerInstitution);
    }

    public static bool IsValidCode(string? code)
    {
        string? normalizedCode = Normalize(code);

        return normalizedCode is null
            || (
                normalizedCode.Length <= MaximumCodeLength
                && !normalizedCode.Contains(
                    ' ',
                    StringComparison.Ordinal)
            );
    }

    public static bool IsValidStatus(string? status)
    {
        string? normalizedStatus = Normalize(status);

        return normalizedStatus is null
            || (
                normalizedStatus.Length <= MaximumStatusLength
                && !normalizedStatus.Contains(
                    ' ',
                    StringComparison.Ordinal)
            );
    }

    public static bool IsValidNotes(string? notes)
    {
        string? normalizedNotes = Normalize(notes);

        return normalizedNotes is null
            || normalizedNotes.Length <= MaximumNotesLength;
    }

    public static bool IsValidSearchTerm(string? searchTerm)
    {
        string? normalizedSearchTerm = Normalize(searchTerm);

        return normalizedSearchTerm is null
            || normalizedSearchTerm.Length
                <= MaximumSearchTermLength;
    }

    public static bool IsValidReason(string? reason)
    {
        string? normalizedReason = Normalize(reason);

        return normalizedReason is not null
            && normalizedReason.Length <= MaximumReasonLength;
    }

    public static bool IsValidOptionalReason(string? reason)
    {
        string? normalizedReason = Normalize(reason);

        return normalizedReason is null
            || normalizedReason.Length <= MaximumReasonLength;
    }

    public static bool IsValidOptionalReferenceNumber(
        string? referenceNumber)
    {
        string? normalizedReferenceNumber =
            Normalize(referenceNumber);

        return normalizedReferenceNumber is null
            || normalizedReferenceNumber.Length
                <= MaximumReferenceNumberLength;
    }

    public static bool IsValidOptionalAmount(
        decimal? amount)
    {
        return amount is null or >= 0;
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
        ArgumentNullException.ThrowIfNull(requestContext);

        return actorUserId > 0
            && requestContext.CorrelationId != Guid.Empty;
    }

    public static string NormalizePeriodicity(
        string? periodicity)
    {
        return Normalize(periodicity)
            ?? DefaultPeriodicity;
    }

    public static string NormalizeRequiredText(
        string value)
    {
        return value.Trim();
    }

    public static string? NormalizeOptionalText(
        string? value)
    {
        return Normalize(value);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
