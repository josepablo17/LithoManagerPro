namespace LithoManager.Application.Features.Documents;

internal static class DocumentMapper
{
    public static DocumentTypeInfo Map(
        DocumentTypeData documentType)
    {
        ArgumentNullException.ThrowIfNull(documentType);

        return new DocumentTypeInfo(
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
                documentType.RowVersion);
    }

    public static EmployeeRecordInfo Map(
        EmployeeRecordData employeeRecord)
    {
        ArgumentNullException.ThrowIfNull(employeeRecord);

        return new EmployeeRecordInfo(
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
                employeeRecord.RowVersion);
    }

    public static EmployeeDocumentInfo Map(
        EmployeeDocumentData employeeDocument)
    {
        ArgumentNullException.ThrowIfNull(employeeDocument);

        return new EmployeeDocumentInfo(
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
                employeeDocument.RowVersion);
    }

    public static EmployeeDocumentDownloadContextInfo Map(
        EmployeeDocumentDownloadContextData downloadContext)
    {
        ArgumentNullException.ThrowIfNull(downloadContext);

        return new EmployeeDocumentDownloadContextInfo(
            EmployeeDocumentId:
                downloadContext.EmployeeDocumentId,
            EmployeeRecordId:
                downloadContext.EmployeeRecordId,
            EmployeeId:
                downloadContext.EmployeeId,
            IdentificationNumber:
                downloadContext.IdentificationNumber,
            FirstName:
                downloadContext.FirstName,
            LastName:
                downloadContext.LastName,
            DocumentTypeId:
                downloadContext.DocumentTypeId,
            DocumentTypeCode:
                downloadContext.DocumentTypeCode,
            DocumentTypeName:
                downloadContext.DocumentTypeName,
            Title:
                downloadContext.Title,
            OriginalFileName:
                downloadContext.OriginalFileName,
            StorageProvider:
                downloadContext.StorageProvider,
            StorageKey:
                downloadContext.StorageKey,
            ContentType:
                downloadContext.ContentType,
            FileSizeBytes:
                downloadContext.FileSizeBytes,
            FileHash:
                downloadContext.FileHash,
            FileHashAlgorithm:
                downloadContext.FileHashAlgorithm,
            IsVisibleToEmployee:
                downloadContext.IsVisibleToEmployee,
            IsActive:
                downloadContext.IsActive,
            RowVersion:
                downloadContext.RowVersion);
    }
}
