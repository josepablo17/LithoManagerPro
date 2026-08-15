namespace LithoManager.Application.Features.Documents;

public sealed class EmployeeRecordData
{
    public int EmployeeRecordId { get; init; }

    public int EmployeeId { get; init; }

    public string IdentificationNumber { get; init; } =
        string.Empty;

    public string FirstName { get; init; } =
        string.Empty;

    public string LastName { get; init; } =
        string.Empty;

    public int DepartmentId { get; init; }

    public string DepartmentCode { get; init; } =
        string.Empty;

    public string DepartmentName { get; init; } =
        string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public int? CreatedByUserId { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public int? UpdatedByUserId { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
