using LithoManager.Application.Features.Documents;

namespace LithoManager.Api.Contracts.Documents;

internal static class DocumentResponseMapper
{
    public static DocumentTypeResponse Map(
        DocumentTypeInfo documentType)
    {
        ArgumentNullException.ThrowIfNull(documentType);

        return new DocumentTypeResponse(
            DocumentTypeId:
                documentType.DocumentTypeId,
            DocumentTypeCode:
                documentType.DocumentTypeCode,
            Name:
                documentType.Name,
            Description:
                documentType.Description,
            DefaultIsVisibleToEmployee:
                documentType.DefaultIsVisibleToEmployee,
            IsActive:
                documentType.IsActive,
            CreatedAtUtc:
                documentType.CreatedAtUtc,
            CreatedByUserId:
                documentType.CreatedByUserId,
            UpdatedAtUtc:
                documentType.UpdatedAtUtc,
            UpdatedByUserId:
                documentType.UpdatedByUserId,
            RowVersion:
                ToRowVersion(documentType.RowVersion));
    }

    public static EmployeeRecordResponse Map(
        EmployeeRecordInfo employeeRecord)
    {
        ArgumentNullException.ThrowIfNull(employeeRecord);

        return new EmployeeRecordResponse(
            EmployeeRecordId:
                employeeRecord.EmployeeRecordId,
            EmployeeId:
                employeeRecord.EmployeeId,
            IdentificationNumber:
                employeeRecord.IdentificationNumber,
            FirstName:
                employeeRecord.FirstName,
            LastName:
                employeeRecord.LastName,
            DepartmentId:
                employeeRecord.DepartmentId,
            DepartmentCode:
                employeeRecord.DepartmentCode,
            DepartmentName:
                employeeRecord.DepartmentName,
            CreatedAtUtc:
                employeeRecord.CreatedAtUtc,
            CreatedByUserId:
                employeeRecord.CreatedByUserId,
            UpdatedAtUtc:
                employeeRecord.UpdatedAtUtc,
            UpdatedByUserId:
                employeeRecord.UpdatedByUserId,
            RowVersion:
                ToRowVersion(employeeRecord.RowVersion));
    }

    public static EmployeeDocumentResponse Map(
        EmployeeDocumentInfo employeeDocument)
    {
        ArgumentNullException.ThrowIfNull(employeeDocument);

        return new EmployeeDocumentResponse(
            EmployeeDocumentId:
                employeeDocument.EmployeeDocumentId,
            EmployeeRecordId:
                employeeDocument.EmployeeRecordId,
            EmployeeId:
                employeeDocument.EmployeeId,
            IdentificationNumber:
                employeeDocument.IdentificationNumber,
            FirstName:
                employeeDocument.FirstName,
            LastName:
                employeeDocument.LastName,
            DepartmentId:
                employeeDocument.DepartmentId,
            DepartmentCode:
                employeeDocument.DepartmentCode,
            DepartmentName:
                employeeDocument.DepartmentName,
            DocumentTypeId:
                employeeDocument.DocumentTypeId,
            DocumentTypeCode:
                employeeDocument.DocumentTypeCode,
            DocumentTypeName:
                employeeDocument.DocumentTypeName,
            Title:
                employeeDocument.Title,
            Description:
                employeeDocument.Description,
            OriginalFileName:
                employeeDocument.OriginalFileName,
            ContentType:
                employeeDocument.ContentType,
            FileSizeBytes:
                employeeDocument.FileSizeBytes,
            FileHashAlgorithm:
                employeeDocument.FileHashAlgorithm,
            IssuedDate:
                employeeDocument.IssuedDate,
            ExpirationDate:
                employeeDocument.ExpirationDate,
            IsVisibleToEmployee:
                employeeDocument.IsVisibleToEmployee,
            IsActive:
                employeeDocument.IsActive,
            DeactivatedAtUtc:
                employeeDocument.DeactivatedAtUtc,
            DeactivatedByUserId:
                employeeDocument.DeactivatedByUserId,
            CreatedAtUtc:
                employeeDocument.CreatedAtUtc,
            CreatedByUserId:
                employeeDocument.CreatedByUserId,
            UpdatedAtUtc:
                employeeDocument.UpdatedAtUtc,
            UpdatedByUserId:
                employeeDocument.UpdatedByUserId,
            RowVersion:
                ToRowVersion(employeeDocument.RowVersion));
    }

    private static string ToRowVersion(
        byte[] rowVersion)
    {
        return Convert.ToBase64String(rowVersion);
    }
}
