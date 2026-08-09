namespace LithoManager.Application.Features
    .HumanResources.Departments;

public sealed class DepartmentData
{
    public int DepartmentId { get; init; }

    public string DepartmentCode { get; init; } =
        string.Empty;

    public string Name { get; init; } =
        string.Empty;

    public string? Description { get; init; }

    public bool IsActive { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public int? CreatedByUserId { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public int? UpdatedByUserId { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
