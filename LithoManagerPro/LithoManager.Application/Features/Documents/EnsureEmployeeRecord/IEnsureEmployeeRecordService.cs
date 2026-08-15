namespace LithoManager.Application.Features
    .Documents.EnsureEmployeeRecord;

public interface IEnsureEmployeeRecordService
{
    Task<EmployeeRecordResult> EnsureAsync(
        EnsureEmployeeRecordCommand command,
        CancellationToken cancellationToken);
}
