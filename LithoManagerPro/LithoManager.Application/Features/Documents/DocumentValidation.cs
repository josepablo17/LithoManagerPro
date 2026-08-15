using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features.Documents;

internal static class DocumentValidation
{
    private const int MaximumTitleLength = 150;
    private const int MaximumDescriptionLength = 500;
    private const int MaximumOriginalFileNameLength = 260;
    private const int MaximumStorageProviderLength = 50;
    private const int MaximumStorageKeyLength = 450;
    private const int MaximumContentTypeLength = 150;
    private const int MaximumSearchTermLength = 150;
    private const int Sha256HashLength = 32;

    public static bool IsValidPositiveId(int value)
    {
        return value > 0;
    }

    public static bool IsValidOptionalPositiveId(int? value)
    {
        return value is null || value > 0;
    }

    public static bool IsValidDateRange(
        DateTime? from,
        DateTime? to)
    {
        return !from.HasValue
            || !to.HasValue
            || to.Value >= from.Value;
    }

    public static bool IsValidDocumentDates(
        DateTime? issuedDate,
        DateTime? expirationDate)
    {
        return !issuedDate.HasValue
            || !expirationDate.HasValue
            || expirationDate.Value.Date
                >= issuedDate.Value.Date;
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

    public static bool IsValidSearchTerm(
        string? searchTerm)
    {
        string? normalizedSearchTerm =
            Normalize(searchTerm);

        return normalizedSearchTerm is null
            || normalizedSearchTerm.Length
                <= MaximumSearchTermLength;
    }

    public static bool IsValidCreateRequest(
        string? title,
        string? description,
        string? originalFileName,
        string? storageProvider,
        string? storageKey,
        string? contentType,
        long? fileSizeBytes,
        byte[]? fileHash,
        DateTime? issuedDate,
        DateTime? expirationDate)
    {
        return IsValidRequiredText(
                title,
                MaximumTitleLength)
            && IsValidOptionalText(
                description,
                MaximumDescriptionLength)
            && IsValidRequiredText(
                originalFileName,
                MaximumOriginalFileNameLength)
            && IsValidRequiredText(
                storageProvider,
                MaximumStorageProviderLength)
            && IsValidRequiredText(
                storageKey,
                MaximumStorageKeyLength)
            && IsValidRequiredText(
                contentType,
                MaximumContentTypeLength)
            && fileSizeBytes is > 0
            && fileHash is { Length: Sha256HashLength }
            && IsValidDocumentDates(
                issuedDate,
                expirationDate);
    }

    public static bool IsValidUpdateRequest(
        string? title,
        string? description,
        DateTime? issuedDate,
        DateTime? expirationDate)
    {
        return IsValidRequiredText(
                title,
                MaximumTitleLength)
            && IsValidOptionalText(
                description,
                MaximumDescriptionLength)
            && IsValidDocumentDates(
                issuedDate,
                expirationDate);
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
