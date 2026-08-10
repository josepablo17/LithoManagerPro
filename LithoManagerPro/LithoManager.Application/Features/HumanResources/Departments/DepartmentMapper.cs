namespace LithoManager.Application.Features
    .HumanResources.Departments;

internal static class DepartmentMapper
{
    public static DepartmentInfo Map(
        DepartmentData department)
    {
        ArgumentNullException.ThrowIfNull(department);

        return new DepartmentInfo(
            DepartmentId:
                department.DepartmentId,
            DepartmentCode:
                department.DepartmentCode,
            Name:
                department.Name,
            Description:
                department.Description,
            IsActive:
                department.IsActive,
            CreatedAtUtc:
                department.CreatedAtUtc,
            CreatedByUserId:
                department.CreatedByUserId,
            UpdatedAtUtc:
                department.UpdatedAtUtc,
            UpdatedByUserId:
                department.UpdatedByUserId,
            RowVersion:
                department.RowVersion);
    }
}
