namespace LithoManager.Application.Features.LeaveManagement;

internal static class LeaveManagementMapper
{
    public static LeaveTypeInfo Map(
        LeaveTypeData leaveType)
    {
        ArgumentNullException.ThrowIfNull(leaveType);

        return new LeaveTypeInfo(
            LeaveTypeId:
                leaveType.LeaveTypeId,
            LeaveTypeCode:
                leaveType.LeaveTypeCode,
            Name:
                leaveType.Name,
            AffectsVacationBalance:
                leaveType.AffectsVacationBalance,
            IsActive:
                leaveType.IsActive,
            CreatedAtUtc:
                leaveType.CreatedAtUtc,
            CreatedByUserId:
                leaveType.CreatedByUserId,
            UpdatedAtUtc:
                leaveType.UpdatedAtUtc,
            UpdatedByUserId:
                leaveType.UpdatedByUserId,
            RowVersion:
                leaveType.RowVersion);
    }

    public static LeaveRequestStatusInfo Map(
        LeaveRequestStatusData status)
    {
        ArgumentNullException.ThrowIfNull(status);

        return new LeaveRequestStatusInfo(
            LeaveRequestStatusCode:
                status.LeaveRequestStatusCode,
            Name:
                status.Name,
            SortOrder:
                status.SortOrder,
            IsTerminal:
                status.IsTerminal,
            IsActive:
                status.IsActive,
            CreatedAtUtc:
                status.CreatedAtUtc,
            UpdatedAtUtc:
                status.UpdatedAtUtc,
            RowVersion:
                status.RowVersion);
    }

    public static EmployeeLeaveBalanceInfo Map(
        EmployeeLeaveBalanceData balance)
    {
        ArgumentNullException.ThrowIfNull(balance);

        return new EmployeeLeaveBalanceInfo(
            EmployeeLeaveBalanceId:
                balance.EmployeeLeaveBalanceId,
            EmployeeId:
                balance.EmployeeId,
            IdentificationNumber:
                balance.IdentificationNumber,
            FirstName:
                balance.FirstName,
            LastName:
                balance.LastName,
            EmployeeName:
                balance.EmployeeName,
            DepartmentId:
                balance.DepartmentId,
            DepartmentCode:
                balance.DepartmentCode,
            DepartmentName:
                balance.DepartmentName,
            LeaveTypeId:
                balance.LeaveTypeId,
            LeaveTypeCode:
                balance.LeaveTypeCode,
            LeaveTypeName:
                balance.LeaveTypeName,
            AffectsVacationBalance:
                balance.AffectsVacationBalance,
            LeavePolicyId:
                balance.LeavePolicyId,
            LeavePolicyCode:
                balance.LeavePolicyCode,
            LeavePolicyName:
                balance.LeavePolicyName,
            EntitlementDays:
                balance.EntitlementDays,
            EntitlementWeeks:
                balance.EntitlementWeeks,
            UsesBusinessDays:
                balance.UsesBusinessDays,
            AccruedDays:
                balance.AccruedDays,
            AdjustedDays:
                balance.AdjustedDays,
            PendingDays:
                balance.PendingDays,
            UsedDays:
                balance.UsedDays,
            AvailableDays:
                balance.AvailableDays,
            CreatedAtUtc:
                balance.CreatedAtUtc,
            CreatedByUserId:
                balance.CreatedByUserId,
            UpdatedAtUtc:
                balance.UpdatedAtUtc,
            UpdatedByUserId:
                balance.UpdatedByUserId,
            RowVersion:
                balance.RowVersion);
    }

    public static LeaveRequestInfo Map(
        LeaveRequestData leaveRequest)
    {
        ArgumentNullException.ThrowIfNull(leaveRequest);

        return new LeaveRequestInfo(
            LeaveRequestId:
                leaveRequest.LeaveRequestId,
            EmployeeId:
                leaveRequest.EmployeeId,
            IdentificationNumber:
                leaveRequest.IdentificationNumber,
            FirstName:
                leaveRequest.FirstName,
            LastName:
                leaveRequest.LastName,
            DepartmentId:
                leaveRequest.DepartmentId,
            DepartmentCode:
                leaveRequest.DepartmentCode,
            DepartmentName:
                leaveRequest.DepartmentName,
            LeaveTypeId:
                leaveRequest.LeaveTypeId,
            LeaveTypeCode:
                leaveRequest.LeaveTypeCode,
            LeaveTypeName:
                leaveRequest.LeaveTypeName,
            LeaveRequestStatusCode:
                leaveRequest.LeaveRequestStatusCode,
            LeaveRequestStatusName:
                leaveRequest.LeaveRequestStatusName,
            StartDate:
                leaveRequest.StartDate,
            EndDate:
                leaveRequest.EndDate,
            RequestedDays:
                leaveRequest.RequestedDays,
            RespondedAtUtc:
                leaveRequest.RespondedAtUtc,
            RespondedByUserId:
                leaveRequest.RespondedByUserId,
            RespondedByEmailAddress:
                leaveRequest.RespondedByEmailAddress,
            CancelledAtUtc:
                leaveRequest.CancelledAtUtc,
            CancelledByUserId:
                leaveRequest.CancelledByUserId,
            CancelledByEmailAddress:
                leaveRequest.CancelledByEmailAddress,
            CreatedAtUtc:
                leaveRequest.CreatedAtUtc,
            CreatedByUserId:
                leaveRequest.CreatedByUserId,
            CreatedByEmailAddress:
                leaveRequest.CreatedByEmailAddress,
            UpdatedAtUtc:
                leaveRequest.UpdatedAtUtc,
            UpdatedByUserId:
                leaveRequest.UpdatedByUserId,
            UpdatedByEmailAddress:
                leaveRequest.UpdatedByEmailAddress,
            RowVersion:
                leaveRequest.RowVersion);
    }
}
