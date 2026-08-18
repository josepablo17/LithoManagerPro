namespace LithoManager.Application.Features.Payroll;

internal static class PayrollMapper
{
    public static PayrollConceptInfo Map(
        PayrollConceptData concept)
    {
        ArgumentNullException.ThrowIfNull(concept);

        return new PayrollConceptInfo(
            concept.PayrollConceptId,
            concept.PayrollConceptCode,
            concept.Name,
            concept.Description,
            concept.ConceptKind,
            concept.IsSystemConcept,
            concept.IsTaxableForIncomeTax,
            concept.IsSubjectToSocialContributions,
            concept.CountsForAguinaldo,
            concept.IsActive,
            concept.CreatedAtUtc,
            concept.CreatedByUserId,
            concept.UpdatedAtUtc,
            concept.UpdatedByUserId,
            concept.RowVersion);
    }

    public static SocialContributionTypeInfo Map(
        SocialContributionTypeData contributionType)
    {
        ArgumentNullException.ThrowIfNull(contributionType);

        return new SocialContributionTypeInfo(
            contributionType.SocialContributionTypeId,
            contributionType.ContributionCode,
            contributionType.Name,
            contributionType.InstitutionName,
            contributionType.ContributionGroup,
            contributionType.AppliesToEmployee,
            contributionType.AppliesToEmployer,
            contributionType.UsesMinimumBase,
            contributionType.IsActive,
            contributionType.CreatedAtUtc,
            contributionType.CreatedByUserId,
            contributionType.UpdatedAtUtc,
            contributionType.UpdatedByUserId,
            contributionType.RowVersion);
    }

    public static SocialContributionRateInfo Map(
        SocialContributionRateData contributionRate)
    {
        ArgumentNullException.ThrowIfNull(contributionRate);

        return new SocialContributionRateInfo(
            contributionRate.SocialContributionRateId,
            contributionRate.SocialContributionTypeId,
            contributionRate.ContributionCode,
            contributionRate.ContributionName,
            contributionRate.InstitutionName,
            contributionRate.ContributionGroup,
            contributionRate.AppliesToEmployee,
            contributionRate.AppliesToEmployer,
            contributionRate.UsesMinimumBase,
            contributionRate.EmployeeRate,
            contributionRate.EmployerRate,
            contributionRate.EffectiveFromDate,
            contributionRate.EffectiveToDate,
            contributionRate.LegalReference,
            contributionRate.CreatedAtUtc,
            contributionRate.CreatedByUserId,
            contributionRate.UpdatedAtUtc,
            contributionRate.UpdatedByUserId,
            contributionRate.RowVersion);
    }

    public static SocialContributionMinimumBaseInfo Map(
        SocialContributionMinimumBaseData minimumBase)
    {
        ArgumentNullException.ThrowIfNull(minimumBase);

        return new SocialContributionMinimumBaseInfo(
            minimumBase.SocialContributionMinimumBaseId,
            minimumBase.SocialContributionTypeId,
            minimumBase.ContributionCode,
            minimumBase.ContributionName,
            minimumBase.InstitutionName,
            minimumBase.ContributionGroup,
            minimumBase.MinimumBaseAmount,
            minimumBase.EffectiveFromDate,
            minimumBase.EffectiveToDate,
            minimumBase.LegalReference,
            minimumBase.CreatedAtUtc,
            minimumBase.CreatedByUserId,
            minimumBase.UpdatedAtUtc,
            minimumBase.UpdatedByUserId,
            minimumBase.RowVersion);
    }

    public static IncomeTaxBracketInfo Map(
        IncomeTaxBracketData bracket)
    {
        ArgumentNullException.ThrowIfNull(bracket);

        return new IncomeTaxBracketInfo(
            bracket.IncomeTaxBracketId,
            bracket.TaxYear,
            bracket.Periodicity,
            bracket.LowerBoundAmount,
            bracket.UpperBoundAmount,
            bracket.TaxRate,
            bracket.EffectiveFromDate,
            bracket.EffectiveToDate,
            bracket.LegalReference,
            bracket.CreatedAtUtc,
            bracket.CreatedByUserId,
            bracket.UpdatedAtUtc,
            bracket.UpdatedByUserId,
            bracket.RowVersion);
    }

    public static IncomeTaxCreditInfo Map(
        IncomeTaxCreditData credit)
    {
        ArgumentNullException.ThrowIfNull(credit);

        return new IncomeTaxCreditInfo(
            credit.IncomeTaxCreditId,
            credit.CreditCode,
            credit.Name,
            credit.TaxYear,
            credit.Periodicity,
            credit.CreditAmount,
            credit.EffectiveFromDate,
            credit.EffectiveToDate,
            credit.LegalReference,
            credit.IsActive,
            credit.CreatedAtUtc,
            credit.CreatedByUserId,
            credit.UpdatedAtUtc,
            credit.UpdatedByUserId,
            credit.RowVersion);
    }

    public static WorkShiftTypeInfo Map(
        WorkShiftTypeData workShiftType)
    {
        ArgumentNullException.ThrowIfNull(workShiftType);

        return new WorkShiftTypeInfo(
            workShiftType.WorkShiftTypeId,
            workShiftType.WorkShiftTypeCode,
            workShiftType.Name,
            workShiftType.MaxOrdinaryHoursPerDay,
            workShiftType.MaxOrdinaryHoursPerWeek,
            workShiftType.MaxTotalHoursPerDay,
            workShiftType.EffectiveFromDate,
            workShiftType.EffectiveToDate,
            workShiftType.IsActive,
            workShiftType.CreatedAtUtc,
            workShiftType.CreatedByUserId,
            workShiftType.UpdatedAtUtc,
            workShiftType.UpdatedByUserId,
            workShiftType.RowVersion);
    }

    public static OvertimeRuleInfo Map(
        OvertimeRuleData overtimeRule)
    {
        ArgumentNullException.ThrowIfNull(overtimeRule);

        return new OvertimeRuleInfo(
            overtimeRule.OvertimeRuleId,
            overtimeRule.OvertimeRuleCode,
            overtimeRule.Name,
            overtimeRule.HourMultiplier,
            overtimeRule.CountsForAguinaldo,
            overtimeRule.EffectiveFromDate,
            overtimeRule.EffectiveToDate,
            overtimeRule.IsActive,
            overtimeRule.CreatedAtUtc,
            overtimeRule.CreatedByUserId,
            overtimeRule.UpdatedAtUtc,
            overtimeRule.UpdatedByUserId,
            overtimeRule.RowVersion);
    }

    public static DisabilityTypeInfo Map(
        DisabilityTypeData disabilityType)
    {
        ArgumentNullException.ThrowIfNull(disabilityType);

        return new DisabilityTypeInfo(
            disabilityType.DisabilityTypeId,
            disabilityType.DisabilityTypeCode,
            disabilityType.Name,
            disabilityType.CountsAsSalaryForAguinaldo,
            disabilityType.RequiresSubsidyTracking,
            disabilityType.ReducesWorkedDays,
            disabilityType.IsActive,
            disabilityType.CreatedAtUtc,
            disabilityType.CreatedByUserId,
            disabilityType.UpdatedAtUtc,
            disabilityType.UpdatedByUserId,
            disabilityType.RowVersion);
    }

    public static AguinaldoRuleInfo Map(
        AguinaldoRuleData aguinaldoRule)
    {
        ArgumentNullException.ThrowIfNull(aguinaldoRule);

        return new AguinaldoRuleInfo(
            aguinaldoRule.AguinaldoRuleId,
            aguinaldoRule.AguinaldoRuleCode,
            aguinaldoRule.Name,
            aguinaldoRule.CalculationStartMonth,
            aguinaldoRule.CalculationStartDay,
            aguinaldoRule.CalculationEndMonth,
            aguinaldoRule.CalculationEndDay,
            aguinaldoRule.Divisor,
            aguinaldoRule.PaymentDueMonth,
            aguinaldoRule.PaymentDueDay,
            aguinaldoRule.IncludesOrdinarySalary,
            aguinaldoRule.IncludesOvertime,
            aguinaldoRule.IncludesSalaryInKind,
            aguinaldoRule.ExcludesCommonIllnessSubsidy,
            aguinaldoRule.IncludesMaternitySubsidy,
            aguinaldoRule.EffectiveFromDate,
            aguinaldoRule.EffectiveToDate,
            aguinaldoRule.IsActive,
            aguinaldoRule.CreatedAtUtc,
            aguinaldoRule.CreatedByUserId,
            aguinaldoRule.UpdatedAtUtc,
            aguinaldoRule.UpdatedByUserId,
            aguinaldoRule.RowVersion);
    }

    public static EmployeeWorkScheduleInfo Map(
        EmployeeWorkScheduleData schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        return new EmployeeWorkScheduleInfo(
            schedule.EmployeeWorkScheduleId,
            schedule.EmployeeId,
            schedule.IdentificationType,
            schedule.IdentificationNumber,
            schedule.FirstName,
            schedule.LastName,
            schedule.WorkShiftTypeId,
            schedule.WorkShiftTypeCode,
            schedule.WorkShiftTypeName,
            schedule.WeeklyOrdinaryHours,
            schedule.WorksMonday,
            schedule.WorksTuesday,
            schedule.WorksWednesday,
            schedule.WorksThursday,
            schedule.WorksFriday,
            schedule.WorksSaturday,
            schedule.WorksSunday,
            schedule.EffectiveFromDate,
            schedule.EffectiveToDate,
            schedule.IsActive,
            schedule.CreatedAtUtc,
            schedule.CreatedByUserId,
            schedule.UpdatedAtUtc,
            schedule.UpdatedByUserId,
            schedule.RowVersion);
    }

    public static AttendanceRecordInfo Map(
        AttendanceRecordData attendanceRecord)
    {
        ArgumentNullException.ThrowIfNull(attendanceRecord);

        return new AttendanceRecordInfo(
            attendanceRecord.AttendanceRecordId,
            attendanceRecord.EmployeeId,
            attendanceRecord.IdentificationType,
            attendanceRecord.IdentificationNumber,
            attendanceRecord.FirstName,
            attendanceRecord.LastName,
            attendanceRecord.WorkShiftTypeId,
            attendanceRecord.WorkShiftTypeCode,
            attendanceRecord.WorkShiftTypeName,
            attendanceRecord.AttendanceDate,
            attendanceRecord.AttendanceStatus,
            attendanceRecord.ExpectedHours,
            attendanceRecord.WorkedHours,
            attendanceRecord.PaidHours,
            attendanceRecord.UnpaidHours,
            attendanceRecord.IsPaidHoliday,
            attendanceRecord.IsApproved,
            attendanceRecord.ApprovedAtUtc,
            attendanceRecord.ApprovedByUserId,
            attendanceRecord.Notes,
            attendanceRecord.CreatedAtUtc,
            attendanceRecord.CreatedByUserId,
            attendanceRecord.UpdatedAtUtc,
            attendanceRecord.UpdatedByUserId,
            attendanceRecord.RowVersion);
    }

    public static OvertimeRecordInfo Map(
        OvertimeRecordData overtimeRecord)
    {
        ArgumentNullException.ThrowIfNull(overtimeRecord);

        return new OvertimeRecordInfo(
            overtimeRecord.OvertimeRecordId,
            overtimeRecord.EmployeeId,
            overtimeRecord.IdentificationType,
            overtimeRecord.IdentificationNumber,
            overtimeRecord.FirstName,
            overtimeRecord.LastName,
            overtimeRecord.AttendanceRecordId,
            overtimeRecord.OvertimeRuleId,
            overtimeRecord.OvertimeRuleCode,
            overtimeRecord.OvertimeRuleName,
            overtimeRecord.HourMultiplier,
            overtimeRecord.OvertimeDate,
            overtimeRecord.Hours,
            overtimeRecord.ApprovalStatus,
            overtimeRecord.ApprovedAtUtc,
            overtimeRecord.ApprovedByUserId,
            overtimeRecord.RejectedAtUtc,
            overtimeRecord.RejectedByUserId,
            overtimeRecord.RejectionReason,
            overtimeRecord.Notes,
            overtimeRecord.CreatedAtUtc,
            overtimeRecord.CreatedByUserId,
            overtimeRecord.UpdatedAtUtc,
            overtimeRecord.UpdatedByUserId,
            overtimeRecord.RowVersion);
    }

    public static EmployeeDisabilityInfo Map(
        EmployeeDisabilityData employeeDisability)
    {
        ArgumentNullException.ThrowIfNull(employeeDisability);

        return new EmployeeDisabilityInfo(
            employeeDisability.EmployeeDisabilityId,
            employeeDisability.EmployeeId,
            employeeDisability.IdentificationType,
            employeeDisability.IdentificationNumber,
            employeeDisability.FirstName,
            employeeDisability.LastName,
            employeeDisability.DisabilityTypeId,
            employeeDisability.DisabilityTypeCode,
            employeeDisability.DisabilityTypeName,
            employeeDisability.CountsAsSalaryForAguinaldo,
            employeeDisability.RequiresSubsidyTracking,
            employeeDisability.ReducesWorkedDays,
            employeeDisability.IssuerInstitution,
            employeeDisability.ReferenceNumber,
            employeeDisability.StartDate,
            employeeDisability.EndDate,
            employeeDisability.ReportedDate,
            employeeDisability.DisabilityStatus,
            employeeDisability.EmployerPaidAmount,
            employeeDisability.SubsidyAmount,
            employeeDisability.ApprovedAtUtc,
            employeeDisability.ApprovedByUserId,
            employeeDisability.CancelledAtUtc,
            employeeDisability.CancelledByUserId,
            employeeDisability.CancellationReason,
            employeeDisability.Notes,
            employeeDisability.CreatedAtUtc,
            employeeDisability.CreatedByUserId,
            employeeDisability.UpdatedAtUtc,
            employeeDisability.UpdatedByUserId,
            employeeDisability.RowVersion);
    }
}
