using LithoManager.Application.Features.Payroll;

namespace LithoManager.Api.Contracts.Payroll;

internal static class PayrollResponseMapper
{
    public static PayrollConceptResponse Map(
        PayrollConceptInfo item)
    {
        return new PayrollConceptResponse(
            item.PayrollConceptId,
            item.PayrollConceptCode,
            item.Name,
            item.Description,
            item.ConceptKind,
            item.IsSystemConcept,
            item.IsTaxableForIncomeTax,
            item.IsSubjectToSocialContributions,
            item.CountsForAguinaldo,
            item.IsActive,
            item.CreatedAtUtc,
            item.CreatedByUserId,
            item.UpdatedAtUtc,
            item.UpdatedByUserId,
            ToRowVersion(item.RowVersion));
    }

    public static SocialContributionTypeResponse Map(
        SocialContributionTypeInfo item)
    {
        return new SocialContributionTypeResponse(
            item.SocialContributionTypeId,
            item.ContributionCode,
            item.Name,
            item.InstitutionName,
            item.ContributionGroup,
            item.AppliesToEmployee,
            item.AppliesToEmployer,
            item.UsesMinimumBase,
            item.IsActive,
            item.CreatedAtUtc,
            item.CreatedByUserId,
            item.UpdatedAtUtc,
            item.UpdatedByUserId,
            ToRowVersion(item.RowVersion));
    }

    public static SocialContributionRateResponse Map(
        SocialContributionRateInfo item)
    {
        return new SocialContributionRateResponse(
            item.SocialContributionRateId,
            item.SocialContributionTypeId,
            item.ContributionCode,
            item.ContributionName,
            item.InstitutionName,
            item.ContributionGroup,
            item.AppliesToEmployee,
            item.AppliesToEmployer,
            item.UsesMinimumBase,
            item.EmployeeRate,
            item.EmployerRate,
            item.EffectiveFromDate,
            item.EffectiveToDate,
            item.LegalReference,
            item.CreatedAtUtc,
            item.CreatedByUserId,
            item.UpdatedAtUtc,
            item.UpdatedByUserId,
            ToRowVersion(item.RowVersion));
    }

    public static SocialContributionMinimumBaseResponse Map(
        SocialContributionMinimumBaseInfo item)
    {
        return new SocialContributionMinimumBaseResponse(
            item.SocialContributionMinimumBaseId,
            item.SocialContributionTypeId,
            item.ContributionCode,
            item.ContributionName,
            item.InstitutionName,
            item.ContributionGroup,
            item.MinimumBaseAmount,
            item.EffectiveFromDate,
            item.EffectiveToDate,
            item.LegalReference,
            item.CreatedAtUtc,
            item.CreatedByUserId,
            item.UpdatedAtUtc,
            item.UpdatedByUserId,
            ToRowVersion(item.RowVersion));
    }

    public static IncomeTaxBracketResponse Map(
        IncomeTaxBracketInfo item)
    {
        return new IncomeTaxBracketResponse(
            item.IncomeTaxBracketId,
            item.TaxYear,
            item.Periodicity,
            item.LowerBoundAmount,
            item.UpperBoundAmount,
            item.TaxRate,
            item.EffectiveFromDate,
            item.EffectiveToDate,
            item.LegalReference,
            item.CreatedAtUtc,
            item.CreatedByUserId,
            item.UpdatedAtUtc,
            item.UpdatedByUserId,
            ToRowVersion(item.RowVersion));
    }

    public static IncomeTaxCreditResponse Map(
        IncomeTaxCreditInfo item)
    {
        return new IncomeTaxCreditResponse(
            item.IncomeTaxCreditId,
            item.CreditCode,
            item.Name,
            item.TaxYear,
            item.Periodicity,
            item.CreditAmount,
            item.EffectiveFromDate,
            item.EffectiveToDate,
            item.LegalReference,
            item.IsActive,
            item.CreatedAtUtc,
            item.CreatedByUserId,
            item.UpdatedAtUtc,
            item.UpdatedByUserId,
            ToRowVersion(item.RowVersion));
    }

    public static WorkShiftTypeResponse Map(
        WorkShiftTypeInfo item)
    {
        return new WorkShiftTypeResponse(
            item.WorkShiftTypeId,
            item.WorkShiftTypeCode,
            item.Name,
            item.MaxOrdinaryHoursPerDay,
            item.MaxOrdinaryHoursPerWeek,
            item.MaxTotalHoursPerDay,
            item.EffectiveFromDate,
            item.EffectiveToDate,
            item.IsActive,
            item.CreatedAtUtc,
            item.CreatedByUserId,
            item.UpdatedAtUtc,
            item.UpdatedByUserId,
            ToRowVersion(item.RowVersion));
    }

    public static OvertimeRuleResponse Map(
        OvertimeRuleInfo item)
    {
        return new OvertimeRuleResponse(
            item.OvertimeRuleId,
            item.OvertimeRuleCode,
            item.Name,
            item.HourMultiplier,
            item.CountsForAguinaldo,
            item.EffectiveFromDate,
            item.EffectiveToDate,
            item.IsActive,
            item.CreatedAtUtc,
            item.CreatedByUserId,
            item.UpdatedAtUtc,
            item.UpdatedByUserId,
            ToRowVersion(item.RowVersion));
    }

    public static DisabilityTypeResponse Map(
        DisabilityTypeInfo item)
    {
        return new DisabilityTypeResponse(
            item.DisabilityTypeId,
            item.DisabilityTypeCode,
            item.Name,
            item.CountsAsSalaryForAguinaldo,
            item.RequiresSubsidyTracking,
            item.ReducesWorkedDays,
            item.IsActive,
            item.CreatedAtUtc,
            item.CreatedByUserId,
            item.UpdatedAtUtc,
            item.UpdatedByUserId,
            ToRowVersion(item.RowVersion));
    }

    public static AguinaldoRuleResponse Map(
        AguinaldoRuleInfo item)
    {
        return new AguinaldoRuleResponse(
            item.AguinaldoRuleId,
            item.AguinaldoRuleCode,
            item.Name,
            item.CalculationStartMonth,
            item.CalculationStartDay,
            item.CalculationEndMonth,
            item.CalculationEndDay,
            item.Divisor,
            item.PaymentDueMonth,
            item.PaymentDueDay,
            item.IncludesOrdinarySalary,
            item.IncludesOvertime,
            item.IncludesSalaryInKind,
            item.ExcludesCommonIllnessSubsidy,
            item.IncludesMaternitySubsidy,
            item.EffectiveFromDate,
            item.EffectiveToDate,
            item.IsActive,
            item.CreatedAtUtc,
            item.CreatedByUserId,
            item.UpdatedAtUtc,
            item.UpdatedByUserId,
            ToRowVersion(item.RowVersion));
    }

    public static EmployeeWorkScheduleResponse Map(
        EmployeeWorkScheduleInfo item)
    {
        return new EmployeeWorkScheduleResponse(
            item.EmployeeWorkScheduleId,
            item.EmployeeId,
            item.IdentificationType,
            item.IdentificationNumber,
            item.FirstName,
            item.LastName,
            item.WorkShiftTypeId,
            item.WorkShiftTypeCode,
            item.WorkShiftTypeName,
            item.WeeklyOrdinaryHours,
            item.WorksMonday,
            item.WorksTuesday,
            item.WorksWednesday,
            item.WorksThursday,
            item.WorksFriday,
            item.WorksSaturday,
            item.WorksSunday,
            item.EffectiveFromDate,
            item.EffectiveToDate,
            item.IsActive,
            item.CreatedAtUtc,
            item.CreatedByUserId,
            item.UpdatedAtUtc,
            item.UpdatedByUserId,
            ToRowVersion(item.RowVersion));
    }

    public static AttendanceRecordResponse Map(
        AttendanceRecordInfo item)
    {
        return new AttendanceRecordResponse(
            item.AttendanceRecordId,
            item.EmployeeId,
            item.IdentificationType,
            item.IdentificationNumber,
            item.FirstName,
            item.LastName,
            item.WorkShiftTypeId,
            item.WorkShiftTypeCode,
            item.WorkShiftTypeName,
            item.AttendanceDate,
            item.AttendanceStatus,
            item.ExpectedHours,
            item.WorkedHours,
            item.PaidHours,
            item.UnpaidHours,
            item.IsPaidHoliday,
            item.IsApproved,
            item.ApprovedAtUtc,
            item.ApprovedByUserId,
            item.Notes,
            item.CreatedAtUtc,
            item.CreatedByUserId,
            item.UpdatedAtUtc,
            item.UpdatedByUserId,
            ToRowVersion(item.RowVersion));
    }

    public static OvertimeRecordResponse Map(
        OvertimeRecordInfo item)
    {
        return new OvertimeRecordResponse(
            item.OvertimeRecordId,
            item.EmployeeId,
            item.IdentificationType,
            item.IdentificationNumber,
            item.FirstName,
            item.LastName,
            item.AttendanceRecordId,
            item.OvertimeRuleId,
            item.OvertimeRuleCode,
            item.OvertimeRuleName,
            item.HourMultiplier,
            item.OvertimeDate,
            item.Hours,
            item.ApprovalStatus,
            item.ApprovedAtUtc,
            item.ApprovedByUserId,
            item.RejectedAtUtc,
            item.RejectedByUserId,
            item.RejectionReason,
            item.Notes,
            item.CreatedAtUtc,
            item.CreatedByUserId,
            item.UpdatedAtUtc,
            item.UpdatedByUserId,
            ToRowVersion(item.RowVersion));
    }

    public static EmployeeDisabilityResponse Map(
        EmployeeDisabilityInfo item)
    {
        return new EmployeeDisabilityResponse(
            item.EmployeeDisabilityId,
            item.EmployeeId,
            item.IdentificationType,
            item.IdentificationNumber,
            item.FirstName,
            item.LastName,
            item.DisabilityTypeId,
            item.DisabilityTypeCode,
            item.DisabilityTypeName,
            item.CountsAsSalaryForAguinaldo,
            item.RequiresSubsidyTracking,
            item.ReducesWorkedDays,
            item.IssuerInstitution,
            item.ReferenceNumber,
            item.StartDate,
            item.EndDate,
            item.ReportedDate,
            item.DisabilityStatus,
            item.EmployerPaidAmount,
            item.SubsidyAmount,
            item.ApprovedAtUtc,
            item.ApprovedByUserId,
            item.CancelledAtUtc,
            item.CancelledByUserId,
            item.CancellationReason,
            item.Notes,
            item.CreatedAtUtc,
            item.CreatedByUserId,
            item.UpdatedAtUtc,
            item.UpdatedByUserId,
            ToRowVersion(item.RowVersion));
    }

    private static string ToRowVersion(byte[] rowVersion)
    {
        return Convert.ToBase64String(rowVersion);
    }
}
