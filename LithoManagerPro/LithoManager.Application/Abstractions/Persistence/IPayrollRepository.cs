using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features.Payroll;

namespace LithoManager.Application.Abstractions.Persistence;

public interface IPayrollRepository
{
    Task<IReadOnlyList<PayrollConceptData>>
        GetPayrollConceptsAsync(
            bool? isActive,
            CancellationToken cancellationToken);

    Task<IReadOnlyList<SocialContributionTypeData>>
        GetSocialContributionTypesAsync(
            bool? isActive,
            CancellationToken cancellationToken);

    Task<IReadOnlyList<SocialContributionRateData>>
        GetSocialContributionRatesAsync(
            DateTime? asOfDate,
            bool? isActive,
            CancellationToken cancellationToken);

    Task<IReadOnlyList<SocialContributionMinimumBaseData>>
        GetSocialContributionMinimumBasesAsync(
            DateTime? asOfDate,
            bool? isActive,
            CancellationToken cancellationToken);

    Task<IReadOnlyList<IncomeTaxBracketData>>
        GetIncomeTaxBracketsAsync(
            int taxYear,
            string periodicity,
            DateTime? asOfDate,
            CancellationToken cancellationToken);

    Task<IReadOnlyList<IncomeTaxCreditData>>
        GetIncomeTaxCreditsAsync(
            int taxYear,
            string periodicity,
            DateTime? asOfDate,
            bool? isActive,
            CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkShiftTypeData>>
        GetWorkShiftTypesAsync(
            DateTime? asOfDate,
            bool? isActive,
            CancellationToken cancellationToken);

    Task<IReadOnlyList<OvertimeRuleData>>
        GetOvertimeRulesAsync(
            DateTime? asOfDate,
            bool? isActive,
            CancellationToken cancellationToken);

    Task<IReadOnlyList<DisabilityTypeData>>
        GetDisabilityTypesAsync(
            bool? isActive,
            CancellationToken cancellationToken);

    Task<IReadOnlyList<AguinaldoRuleData>>
        GetAguinaldoRulesAsync(
            DateTime? asOfDate,
            bool? isActive,
            CancellationToken cancellationToken);

    Task<EmployeeWorkScheduleData> SetEmployeeWorkScheduleAsync(
        int employeeId,
        int workShiftTypeId,
        DateTime effectiveFromDate,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AttendanceRecordData>>
        GetAttendanceRecordsAsync(
            int actorUserId,
            int? employeeId,
            int? departmentId,
            string? attendanceStatus,
            bool? isApproved,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? searchTerm,
            CancellationToken cancellationToken);

    Task<AttendanceRecordData?> GetAttendanceRecordByIdAsync(
        int attendanceRecordId,
        int actorUserId,
        CancellationToken cancellationToken);

    Task<AttendanceRecordData> SaveAttendanceRecordAsync(
        int employeeId,
        DateTime attendanceDate,
        string attendanceStatus,
        decimal expectedHours,
        decimal workedHours,
        decimal paidHours,
        decimal unpaidHours,
        int? workShiftTypeId,
        bool isPaidHoliday,
        bool isApproved,
        string? notes,
        byte[]? expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OvertimeRecordData>>
        GetOvertimeRecordsAsync(
            int actorUserId,
            int? employeeId,
            int? departmentId,
            int? overtimeRuleId,
            string? approvalStatus,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? searchTerm,
            CancellationToken cancellationToken);

    Task<OvertimeRecordData?> GetOvertimeRecordByIdAsync(
        int overtimeRecordId,
        int actorUserId,
        CancellationToken cancellationToken);

    Task<OvertimeRecordData> CreateOvertimeRecordAsync(
        int employeeId,
        int overtimeRuleId,
        DateTime overtimeDate,
        decimal hours,
        int? attendanceRecordId,
        string? notes,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<OvertimeRecordData> RespondOvertimeRecordAsync(
        int overtimeRecordId,
        bool isApproved,
        string? rejectionReason,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<OvertimeRecordData> CancelOvertimeRecordAsync(
        int overtimeRecordId,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EmployeeDisabilityData>>
        GetEmployeeDisabilitiesAsync(
            int actorUserId,
            int? employeeId,
            int? departmentId,
            int? disabilityTypeId,
            string? disabilityStatus,
            string? issuerInstitution,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? searchTerm,
            CancellationToken cancellationToken);

    Task<EmployeeDisabilityData?>
        GetEmployeeDisabilityByIdAsync(
            int employeeDisabilityId,
            int actorUserId,
            CancellationToken cancellationToken);

    Task<EmployeeDisabilityData> CreateEmployeeDisabilityAsync(
        int employeeId,
        int disabilityTypeId,
        string issuerInstitution,
        DateTime startDate,
        DateTime endDate,
        string? referenceNumber,
        decimal? employerPaidAmount,
        decimal? subsidyAmount,
        string? notes,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<EmployeeDisabilityData> ApproveEmployeeDisabilityAsync(
        int employeeDisabilityId,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<EmployeeDisabilityData> CancelEmployeeDisabilityAsync(
        int employeeDisabilityId,
        string cancellationReason,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);
}
