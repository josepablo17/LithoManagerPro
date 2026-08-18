SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @PayrollConcepts TABLE
(
    PayrollConceptCode nvarchar(50) NOT NULL,
    Name nvarchar(100) NOT NULL,
    Description nvarchar(300) NOT NULL,
    ConceptKind nvarchar(30) NOT NULL,
    IsSystemConcept bit NOT NULL,
    IsTaxableForIncomeTax bit NOT NULL,
    IsSubjectToSocialContributions bit NOT NULL,
    CountsForAguinaldo bit NOT NULL,
    IsActive bit NOT NULL
);

INSERT INTO @PayrollConcepts
(
    PayrollConceptCode,
    Name,
    Description,
    ConceptKind,
    IsSystemConcept,
    IsTaxableForIncomeTax,
    IsSubjectToSocialContributions,
    CountsForAguinaldo,
    IsActive
)
VALUES
    (N'BaseSalary', N'Base Salary', N'Regular salary used as the base payroll earning.', N'Earning', 1, 1, 1, 1, 1),
    (N'Overtime', N'Overtime', N'Approved overtime earning.', N'Earning', 1, 1, 1, 1, 1),
    (N'SalaryInKind', N'Salary In Kind', N'Salary in kind used for legal payroll calculations.', N'Earning', 1, 1, 1, 1, 1),
    (N'Aguinaldo', N'Aguinaldo', N'Costa Rica thirteenth-month salary payment.', N'Earning', 1, 0, 0, 0, 1),
    (N'EmployeeSocialContribution', N'Employee Social Contribution', N'Employee-side social contribution deduction.', N'Deduction', 1, 0, 0, 0, 1),
    (N'IncomeTax', N'Income Tax', N'Income tax withheld from salary.', N'Deduction', 1, 0, 0, 0, 1),
    (N'EmployerSocialContribution', N'Employer Social Contribution', N'Employer-side payroll cost.', N'EmployerContribution', 1, 0, 0, 0, 1),
    (N'DisabilitySubsidy', N'Disability Subsidy', N'Disability subsidy amount tracked for payroll and legal reports.', N'Informational', 1, 0, 0, 0, 1);

UPDATE T
SET
    Name = S.Name,
    Description = S.Description,
    ConceptKind = S.ConceptKind,
    IsSystemConcept = S.IsSystemConcept,
    IsTaxableForIncomeTax = S.IsTaxableForIncomeTax,
    IsSubjectToSocialContributions =
        S.IsSubjectToSocialContributions,
    CountsForAguinaldo = S.CountsForAguinaldo,
    IsActive = S.IsActive,
    UpdatedAtUtc = SYSUTCDATETIME()
FROM Payroll.PayrollConcepts AS T
INNER JOIN @PayrollConcepts AS S
    ON S.PayrollConceptCode = T.PayrollConceptCode
WHERE EXISTS
(
    SELECT
        S.Name,
        S.Description,
        S.ConceptKind,
        S.IsSystemConcept,
        S.IsTaxableForIncomeTax,
        S.IsSubjectToSocialContributions,
        S.CountsForAguinaldo,
        S.IsActive
    EXCEPT
    SELECT
        T.Name,
        T.Description,
        T.ConceptKind,
        T.IsSystemConcept,
        T.IsTaxableForIncomeTax,
        T.IsSubjectToSocialContributions,
        T.CountsForAguinaldo,
        T.IsActive
);

INSERT INTO Payroll.PayrollConcepts
(
    PayrollConceptCode,
    Name,
    Description,
    ConceptKind,
    IsSystemConcept,
    IsTaxableForIncomeTax,
    IsSubjectToSocialContributions,
    CountsForAguinaldo,
    IsActive
)
SELECT
    S.PayrollConceptCode,
    S.Name,
    S.Description,
    S.ConceptKind,
    S.IsSystemConcept,
    S.IsTaxableForIncomeTax,
    S.IsSubjectToSocialContributions,
    S.CountsForAguinaldo,
    S.IsActive
FROM @PayrollConcepts AS S
WHERE NOT EXISTS
(
    SELECT 1
    FROM Payroll.PayrollConcepts AS T
    WHERE T.PayrollConceptCode = S.PayrollConceptCode
);

DECLARE @ContributionTypes TABLE
(
    ContributionCode nvarchar(50) NOT NULL,
    Name nvarchar(100) NOT NULL,
    InstitutionName nvarchar(100) NOT NULL,
    ContributionGroup nvarchar(30) NOT NULL,
    AppliesToEmployee bit NOT NULL,
    AppliesToEmployer bit NOT NULL,
    UsesMinimumBase bit NOT NULL,
    IsActive bit NOT NULL
);

INSERT INTO @ContributionTypes
(
    ContributionCode,
    Name,
    InstitutionName,
    ContributionGroup,
    AppliesToEmployee,
    AppliesToEmployer,
    UsesMinimumBase,
    IsActive
)
VALUES
    (N'SEM', N'Sickness and Maternity Insurance', N'CCSS', N'CCSS', 1, 1, 1, 1),
    (N'IVM', N'Disability Old Age and Death Insurance', N'CCSS', N'CCSS', 1, 1, 1, 1),
    (N'BancoPopularEmployerQuota', N'Banco Popular Employer Quota', N'Banco Popular', N'OtherInstitution', 0, 1, 0, 1),
    (N'FamilyAllowances', N'Family Allowances', N'FODESAF', N'OtherInstitution', 0, 1, 0, 1),
    (N'IMAS', N'IMAS Contribution', N'IMAS', N'OtherInstitution', 0, 1, 0, 1),
    (N'INA', N'INA Contribution', N'INA', N'OtherInstitution', 0, 1, 0, 1),
    (N'BancoPopularLpt', N'Banco Popular LPT Contribution', N'Banco Popular', N'LPT', 1, 1, 0, 1),
    (N'FCL', N'Labor Capitalization Fund', N'Operadora de Pensiones', N'LPT', 0, 1, 0, 1),
    (N'OPC', N'Complementary Pension Fund', N'Operadora de Pensiones', N'LPT', 0, 1, 0, 1),
    (N'INS', N'INS Contribution', N'INS', N'LPT', 0, 1, 0, 1);

UPDATE T
SET
    Name = S.Name,
    InstitutionName = S.InstitutionName,
    ContributionGroup = S.ContributionGroup,
    AppliesToEmployee = S.AppliesToEmployee,
    AppliesToEmployer = S.AppliesToEmployer,
    UsesMinimumBase = S.UsesMinimumBase,
    IsActive = S.IsActive,
    UpdatedAtUtc = SYSUTCDATETIME()
FROM Payroll.SocialContributionTypes AS T
INNER JOIN @ContributionTypes AS S
    ON S.ContributionCode = T.ContributionCode
WHERE EXISTS
(
    SELECT
        S.Name,
        S.InstitutionName,
        S.ContributionGroup,
        S.AppliesToEmployee,
        S.AppliesToEmployer,
        S.UsesMinimumBase,
        S.IsActive
    EXCEPT
    SELECT
        T.Name,
        T.InstitutionName,
        T.ContributionGroup,
        T.AppliesToEmployee,
        T.AppliesToEmployer,
        T.UsesMinimumBase,
        T.IsActive
);

INSERT INTO Payroll.SocialContributionTypes
(
    ContributionCode,
    Name,
    InstitutionName,
    ContributionGroup,
    AppliesToEmployee,
    AppliesToEmployer,
    UsesMinimumBase,
    IsActive
)
SELECT
    S.ContributionCode,
    S.Name,
    S.InstitutionName,
    S.ContributionGroup,
    S.AppliesToEmployee,
    S.AppliesToEmployer,
    S.UsesMinimumBase,
    S.IsActive
FROM @ContributionTypes AS S
WHERE NOT EXISTS
(
    SELECT 1
    FROM Payroll.SocialContributionTypes AS T
    WHERE T.ContributionCode = S.ContributionCode
);

DECLARE @ContributionRates TABLE
(
    ContributionCode nvarchar(50) NOT NULL,
    EmployeeRate decimal(9,6) NOT NULL,
    EmployerRate decimal(9,6) NOT NULL,
    EffectiveFromDate date NOT NULL,
    EffectiveToDate date NULL,
    LegalReference nvarchar(300) NULL
);

INSERT INTO @ContributionRates
(
    ContributionCode,
    EmployeeRate,
    EmployerRate,
    EffectiveFromDate,
    EffectiveToDate,
    LegalReference
)
VALUES
    (N'SEM', 0.055000, 0.092500, '2026-01-01', NULL, N'CCSS Patronos 2026'),
    (N'IVM', 0.043300, 0.055800, '2026-01-01', NULL, N'CCSS IVM increase effective 2026'),
    (N'BancoPopularEmployerQuota', 0.000000, 0.002500, '2026-01-01', NULL, N'CCSS Patronos 2026'),
    (N'FamilyAllowances', 0.000000, 0.050000, '2026-01-01', NULL, N'CCSS Patronos 2026'),
    (N'IMAS', 0.000000, 0.005000, '2026-01-01', NULL, N'CCSS Patronos 2026'),
    (N'INA', 0.000000, 0.015000, '2026-01-01', NULL, N'CCSS Patronos 2026'),
    (N'BancoPopularLpt', 0.010000, 0.002500, '2026-01-01', NULL, N'CCSS Patronos 2026'),
    (N'FCL', 0.000000, 0.015000, '2026-01-01', NULL, N'CCSS Patronos 2026'),
    (N'OPC', 0.000000, 0.020000, '2026-01-01', NULL, N'CCSS Patronos 2026'),
    (N'INS', 0.000000, 0.010000, '2026-01-01', NULL, N'CCSS Patronos 2026');

UPDATE T
SET
    EmployeeRate = S.EmployeeRate,
    EmployerRate = S.EmployerRate,
    EffectiveToDate = S.EffectiveToDate,
    LegalReference = S.LegalReference,
    UpdatedAtUtc = SYSUTCDATETIME()
FROM Payroll.SocialContributionRates AS T
INNER JOIN Payroll.SocialContributionTypes AS CT
    ON CT.SocialContributionTypeId =
        T.SocialContributionTypeId
INNER JOIN @ContributionRates AS S
    ON S.ContributionCode = CT.ContributionCode
    AND S.EffectiveFromDate = T.EffectiveFromDate
WHERE EXISTS
(
    SELECT
        S.EmployeeRate,
        S.EmployerRate,
        S.EffectiveToDate,
        S.LegalReference
    EXCEPT
    SELECT
        T.EmployeeRate,
        T.EmployerRate,
        T.EffectiveToDate,
        T.LegalReference
);

INSERT INTO Payroll.SocialContributionRates
(
    SocialContributionTypeId,
    EmployeeRate,
    EmployerRate,
    EffectiveFromDate,
    EffectiveToDate,
    LegalReference
)
SELECT
    CT.SocialContributionTypeId,
    S.EmployeeRate,
    S.EmployerRate,
    S.EffectiveFromDate,
    S.EffectiveToDate,
    S.LegalReference
FROM @ContributionRates AS S
INNER JOIN Payroll.SocialContributionTypes AS CT
    ON CT.ContributionCode = S.ContributionCode
WHERE NOT EXISTS
(
    SELECT 1
    FROM Payroll.SocialContributionRates AS T
    WHERE
        T.SocialContributionTypeId =
            CT.SocialContributionTypeId
        AND T.EffectiveFromDate = S.EffectiveFromDate
);

DECLARE @MinimumBases TABLE
(
    ContributionCode nvarchar(50) NOT NULL,
    MinimumBaseAmount decimal(18,2) NOT NULL,
    EffectiveFromDate date NOT NULL,
    EffectiveToDate date NULL,
    LegalReference nvarchar(300) NULL
);

INSERT INTO @MinimumBases
(
    ContributionCode,
    MinimumBaseAmount,
    EffectiveFromDate,
    EffectiveToDate,
    LegalReference
)
VALUES
    (N'SEM', 346789.00, '2026-01-01', NULL, N'CCSS Patronos 2026 minimum contribution base'),
    (N'IVM', 324590.00, '2026-01-01', NULL, N'CCSS Patronos 2026 minimum contribution base');

UPDATE T
SET
    MinimumBaseAmount = S.MinimumBaseAmount,
    EffectiveToDate = S.EffectiveToDate,
    LegalReference = S.LegalReference,
    UpdatedAtUtc = SYSUTCDATETIME()
FROM Payroll.SocialContributionMinimumBases AS T
INNER JOIN Payroll.SocialContributionTypes AS CT
    ON CT.SocialContributionTypeId =
        T.SocialContributionTypeId
INNER JOIN @MinimumBases AS S
    ON S.ContributionCode = CT.ContributionCode
    AND S.EffectiveFromDate = T.EffectiveFromDate
WHERE EXISTS
(
    SELECT
        S.MinimumBaseAmount,
        S.EffectiveToDate,
        S.LegalReference
    EXCEPT
    SELECT
        T.MinimumBaseAmount,
        T.EffectiveToDate,
        T.LegalReference
);

INSERT INTO Payroll.SocialContributionMinimumBases
(
    SocialContributionTypeId,
    MinimumBaseAmount,
    EffectiveFromDate,
    EffectiveToDate,
    LegalReference
)
SELECT
    CT.SocialContributionTypeId,
    S.MinimumBaseAmount,
    S.EffectiveFromDate,
    S.EffectiveToDate,
    S.LegalReference
FROM @MinimumBases AS S
INNER JOIN Payroll.SocialContributionTypes AS CT
    ON CT.ContributionCode = S.ContributionCode
WHERE NOT EXISTS
(
    SELECT 1
    FROM Payroll.SocialContributionMinimumBases AS T
    WHERE
        T.SocialContributionTypeId =
            CT.SocialContributionTypeId
        AND T.EffectiveFromDate = S.EffectiveFromDate
);

DECLARE @IncomeTaxBrackets TABLE
(
    TaxYear smallint NOT NULL,
    Periodicity nvarchar(20) NOT NULL,
    LowerBoundAmount decimal(18,2) NOT NULL,
    UpperBoundAmount decimal(18,2) NULL,
    TaxRate decimal(9,6) NOT NULL,
    EffectiveFromDate date NOT NULL,
    EffectiveToDate date NULL,
    LegalReference nvarchar(300) NULL
);

INSERT INTO @IncomeTaxBrackets
(
    TaxYear,
    Periodicity,
    LowerBoundAmount,
    UpperBoundAmount,
    TaxRate,
    EffectiveFromDate,
    EffectiveToDate,
    LegalReference
)
VALUES
    (2026, N'Monthly', 0.00, 918000.00, 0.000000, '2026-01-01', NULL, N'Decreto Ejecutivo 45333-H'),
    (2026, N'Monthly', 918000.00, 1347000.00, 0.100000, '2026-01-01', NULL, N'Decreto Ejecutivo 45333-H'),
    (2026, N'Monthly', 1347000.00, 2364000.00, 0.150000, '2026-01-01', NULL, N'Decreto Ejecutivo 45333-H'),
    (2026, N'Monthly', 2364000.00, 4727000.00, 0.200000, '2026-01-01', NULL, N'Decreto Ejecutivo 45333-H'),
    (2026, N'Monthly', 4727000.00, NULL, 0.250000, '2026-01-01', NULL, N'Decreto Ejecutivo 45333-H');

UPDATE T
SET
    UpperBoundAmount = S.UpperBoundAmount,
    TaxRate = S.TaxRate,
    EffectiveFromDate = S.EffectiveFromDate,
    EffectiveToDate = S.EffectiveToDate,
    LegalReference = S.LegalReference,
    UpdatedAtUtc = SYSUTCDATETIME()
FROM Payroll.IncomeTaxBrackets AS T
INNER JOIN @IncomeTaxBrackets AS S
    ON S.TaxYear = T.TaxYear
    AND S.Periodicity = T.Periodicity
    AND S.LowerBoundAmount = T.LowerBoundAmount
WHERE EXISTS
(
    SELECT
        S.UpperBoundAmount,
        S.TaxRate,
        S.EffectiveFromDate,
        S.EffectiveToDate,
        S.LegalReference
    EXCEPT
    SELECT
        T.UpperBoundAmount,
        T.TaxRate,
        T.EffectiveFromDate,
        T.EffectiveToDate,
        T.LegalReference
);

INSERT INTO Payroll.IncomeTaxBrackets
(
    TaxYear,
    Periodicity,
    LowerBoundAmount,
    UpperBoundAmount,
    TaxRate,
    EffectiveFromDate,
    EffectiveToDate,
    LegalReference
)
SELECT
    S.TaxYear,
    S.Periodicity,
    S.LowerBoundAmount,
    S.UpperBoundAmount,
    S.TaxRate,
    S.EffectiveFromDate,
    S.EffectiveToDate,
    S.LegalReference
FROM @IncomeTaxBrackets AS S
WHERE NOT EXISTS
(
    SELECT 1
    FROM Payroll.IncomeTaxBrackets AS T
    WHERE
        T.TaxYear = S.TaxYear
        AND T.Periodicity = S.Periodicity
        AND T.LowerBoundAmount = S.LowerBoundAmount
);

DECLARE @IncomeTaxCredits TABLE
(
    CreditCode nvarchar(50) NOT NULL,
    Name nvarchar(100) NOT NULL,
    TaxYear smallint NOT NULL,
    Periodicity nvarchar(20) NOT NULL,
    CreditAmount decimal(18,2) NOT NULL,
    EffectiveFromDate date NOT NULL,
    EffectiveToDate date NULL,
    LegalReference nvarchar(300) NULL,
    IsActive bit NOT NULL
);

INSERT INTO @IncomeTaxCredits
(
    CreditCode,
    Name,
    TaxYear,
    Periodicity,
    CreditAmount,
    EffectiveFromDate,
    EffectiveToDate,
    LegalReference,
    IsActive
)
VALUES
    (N'Child', N'Child Tax Credit', 2026, N'Monthly', 1710.00, '2026-01-01', NULL, N'Decreto Ejecutivo 45333-H article 2', 1),
    (N'Spouse', N'Spouse Tax Credit', 2026, N'Monthly', 2590.00, '2026-01-01', NULL, N'Decreto Ejecutivo 45333-H article 2', 1);

UPDATE T
SET
    Name = S.Name,
    CreditAmount = S.CreditAmount,
    EffectiveToDate = S.EffectiveToDate,
    LegalReference = S.LegalReference,
    IsActive = S.IsActive,
    UpdatedAtUtc = SYSUTCDATETIME()
FROM Payroll.IncomeTaxCredits AS T
INNER JOIN @IncomeTaxCredits AS S
    ON S.CreditCode = T.CreditCode
    AND S.TaxYear = T.TaxYear
    AND S.Periodicity = T.Periodicity
    AND S.EffectiveFromDate = T.EffectiveFromDate
WHERE EXISTS
(
    SELECT
        S.Name,
        S.CreditAmount,
        S.EffectiveToDate,
        S.LegalReference,
        S.IsActive
    EXCEPT
    SELECT
        T.Name,
        T.CreditAmount,
        T.EffectiveToDate,
        T.LegalReference,
        T.IsActive
);

INSERT INTO Payroll.IncomeTaxCredits
(
    CreditCode,
    Name,
    TaxYear,
    Periodicity,
    CreditAmount,
    EffectiveFromDate,
    EffectiveToDate,
    LegalReference,
    IsActive
)
SELECT
    S.CreditCode,
    S.Name,
    S.TaxYear,
    S.Periodicity,
    S.CreditAmount,
    S.EffectiveFromDate,
    S.EffectiveToDate,
    S.LegalReference,
    S.IsActive
FROM @IncomeTaxCredits AS S
WHERE NOT EXISTS
(
    SELECT 1
    FROM Payroll.IncomeTaxCredits AS T
    WHERE
        T.CreditCode = S.CreditCode
        AND T.TaxYear = S.TaxYear
        AND T.Periodicity = S.Periodicity
        AND T.EffectiveFromDate = S.EffectiveFromDate
);

DECLARE @WorkShiftTypes TABLE
(
    WorkShiftTypeCode nvarchar(50) NOT NULL,
    Name nvarchar(100) NOT NULL,
    MaxOrdinaryHoursPerDay decimal(5,2) NOT NULL,
    MaxOrdinaryHoursPerWeek decimal(5,2) NOT NULL,
    MaxTotalHoursPerDay decimal(5,2) NOT NULL,
    EffectiveFromDate date NOT NULL,
    EffectiveToDate date NULL,
    IsActive bit NOT NULL
);

INSERT INTO @WorkShiftTypes
(
    WorkShiftTypeCode,
    Name,
    MaxOrdinaryHoursPerDay,
    MaxOrdinaryHoursPerWeek,
    MaxTotalHoursPerDay,
    EffectiveFromDate,
    EffectiveToDate,
    IsActive
)
VALUES
    (N'Day', N'Day Shift', 8.00, 48.00, 12.00, '2026-01-01', NULL, 1),
    (N'Mixed', N'Mixed Shift', 7.00, 42.00, 12.00, '2026-01-01', NULL, 1),
    (N'Night', N'Night Shift', 6.00, 36.00, 12.00, '2026-01-01', NULL, 1);

UPDATE T
SET
    Name = S.Name,
    MaxOrdinaryHoursPerDay = S.MaxOrdinaryHoursPerDay,
    MaxOrdinaryHoursPerWeek = S.MaxOrdinaryHoursPerWeek,
    MaxTotalHoursPerDay = S.MaxTotalHoursPerDay,
    EffectiveToDate = S.EffectiveToDate,
    IsActive = S.IsActive,
    UpdatedAtUtc = SYSUTCDATETIME()
FROM Payroll.WorkShiftTypes AS T
INNER JOIN @WorkShiftTypes AS S
    ON S.WorkShiftTypeCode = T.WorkShiftTypeCode
    AND S.EffectiveFromDate = T.EffectiveFromDate
WHERE EXISTS
(
    SELECT
        S.Name,
        S.MaxOrdinaryHoursPerDay,
        S.MaxOrdinaryHoursPerWeek,
        S.MaxTotalHoursPerDay,
        S.EffectiveToDate,
        S.IsActive
    EXCEPT
    SELECT
        T.Name,
        T.MaxOrdinaryHoursPerDay,
        T.MaxOrdinaryHoursPerWeek,
        T.MaxTotalHoursPerDay,
        T.EffectiveToDate,
        T.IsActive
);

INSERT INTO Payroll.WorkShiftTypes
(
    WorkShiftTypeCode,
    Name,
    MaxOrdinaryHoursPerDay,
    MaxOrdinaryHoursPerWeek,
    MaxTotalHoursPerDay,
    EffectiveFromDate,
    EffectiveToDate,
    IsActive
)
SELECT
    S.WorkShiftTypeCode,
    S.Name,
    S.MaxOrdinaryHoursPerDay,
    S.MaxOrdinaryHoursPerWeek,
    S.MaxTotalHoursPerDay,
    S.EffectiveFromDate,
    S.EffectiveToDate,
    S.IsActive
FROM @WorkShiftTypes AS S
WHERE NOT EXISTS
(
    SELECT 1
    FROM Payroll.WorkShiftTypes AS T
    WHERE
        T.WorkShiftTypeCode = S.WorkShiftTypeCode
        AND T.EffectiveFromDate = S.EffectiveFromDate
);

DECLARE @OvertimeRules TABLE
(
    OvertimeRuleCode nvarchar(50) NOT NULL,
    Name nvarchar(100) NOT NULL,
    HourMultiplier decimal(9,4) NOT NULL,
    CountsForAguinaldo bit NOT NULL,
    EffectiveFromDate date NOT NULL,
    EffectiveToDate date NULL,
    IsActive bit NOT NULL
);

INSERT INTO @OvertimeRules
(
    OvertimeRuleCode,
    Name,
    HourMultiplier,
    CountsForAguinaldo,
    EffectiveFromDate,
    EffectiveToDate,
    IsActive
)
VALUES
    (N'Standard', N'Standard Overtime', 1.5000, 1, '2026-01-01', NULL, 1),
    (N'Holiday', N'Holiday Overtime', 2.0000, 1, '2026-01-01', NULL, 1);

UPDATE T
SET
    Name = S.Name,
    HourMultiplier = S.HourMultiplier,
    CountsForAguinaldo = S.CountsForAguinaldo,
    EffectiveToDate = S.EffectiveToDate,
    IsActive = S.IsActive,
    UpdatedAtUtc = SYSUTCDATETIME()
FROM Payroll.OvertimeRules AS T
INNER JOIN @OvertimeRules AS S
    ON S.OvertimeRuleCode = T.OvertimeRuleCode
    AND S.EffectiveFromDate = T.EffectiveFromDate
WHERE EXISTS
(
    SELECT
        S.Name,
        S.HourMultiplier,
        S.CountsForAguinaldo,
        S.EffectiveToDate,
        S.IsActive
    EXCEPT
    SELECT
        T.Name,
        T.HourMultiplier,
        T.CountsForAguinaldo,
        T.EffectiveToDate,
        T.IsActive
);

INSERT INTO Payroll.OvertimeRules
(
    OvertimeRuleCode,
    Name,
    HourMultiplier,
    CountsForAguinaldo,
    EffectiveFromDate,
    EffectiveToDate,
    IsActive
)
SELECT
    S.OvertimeRuleCode,
    S.Name,
    S.HourMultiplier,
    S.CountsForAguinaldo,
    S.EffectiveFromDate,
    S.EffectiveToDate,
    S.IsActive
FROM @OvertimeRules AS S
WHERE NOT EXISTS
(
    SELECT 1
    FROM Payroll.OvertimeRules AS T
    WHERE
        T.OvertimeRuleCode = S.OvertimeRuleCode
        AND T.EffectiveFromDate = S.EffectiveFromDate
);

DECLARE @DisabilityTypes TABLE
(
    DisabilityTypeCode nvarchar(50) NOT NULL,
    Name nvarchar(100) NOT NULL,
    CountsAsSalaryForAguinaldo bit NOT NULL,
    RequiresSubsidyTracking bit NOT NULL,
    ReducesWorkedDays bit NOT NULL,
    IsActive bit NOT NULL
);

INSERT INTO @DisabilityTypes
(
    DisabilityTypeCode,
    Name,
    CountsAsSalaryForAguinaldo,
    RequiresSubsidyTracking,
    ReducesWorkedDays,
    IsActive
)
VALUES
    (N'CommonIllness', N'Common Illness', 0, 1, 1, 1),
    (N'OccupationalRisk', N'Occupational Risk', 0, 1, 1, 1),
    (N'Maternity', N'Maternity Leave', 1, 1, 1, 1);

UPDATE T
SET
    Name = S.Name,
    CountsAsSalaryForAguinaldo =
        S.CountsAsSalaryForAguinaldo,
    RequiresSubsidyTracking = S.RequiresSubsidyTracking,
    ReducesWorkedDays = S.ReducesWorkedDays,
    IsActive = S.IsActive,
    UpdatedAtUtc = SYSUTCDATETIME()
FROM Payroll.DisabilityTypes AS T
INNER JOIN @DisabilityTypes AS S
    ON S.DisabilityTypeCode = T.DisabilityTypeCode
WHERE EXISTS
(
    SELECT
        S.Name,
        S.CountsAsSalaryForAguinaldo,
        S.RequiresSubsidyTracking,
        S.ReducesWorkedDays,
        S.IsActive
    EXCEPT
    SELECT
        T.Name,
        T.CountsAsSalaryForAguinaldo,
        T.RequiresSubsidyTracking,
        T.ReducesWorkedDays,
        T.IsActive
);

INSERT INTO Payroll.DisabilityTypes
(
    DisabilityTypeCode,
    Name,
    CountsAsSalaryForAguinaldo,
    RequiresSubsidyTracking,
    ReducesWorkedDays,
    IsActive
)
SELECT
    S.DisabilityTypeCode,
    S.Name,
    S.CountsAsSalaryForAguinaldo,
    S.RequiresSubsidyTracking,
    S.ReducesWorkedDays,
    S.IsActive
FROM @DisabilityTypes AS S
WHERE NOT EXISTS
(
    SELECT 1
    FROM Payroll.DisabilityTypes AS T
    WHERE T.DisabilityTypeCode = S.DisabilityTypeCode
);

DECLARE @AguinaldoRules TABLE
(
    AguinaldoRuleCode nvarchar(50) NOT NULL,
    Name nvarchar(100) NOT NULL,
    CalculationStartMonth tinyint NOT NULL,
    CalculationStartDay tinyint NOT NULL,
    CalculationEndMonth tinyint NOT NULL,
    CalculationEndDay tinyint NOT NULL,
    Divisor smallint NOT NULL,
    PaymentDueMonth tinyint NOT NULL,
    PaymentDueDay tinyint NOT NULL,
    IncludesOrdinarySalary bit NOT NULL,
    IncludesOvertime bit NOT NULL,
    IncludesSalaryInKind bit NOT NULL,
    ExcludesCommonIllnessSubsidy bit NOT NULL,
    IncludesMaternitySubsidy bit NOT NULL,
    EffectiveFromDate date NOT NULL,
    EffectiveToDate date NULL,
    IsActive bit NOT NULL
);

INSERT INTO @AguinaldoRules
(
    AguinaldoRuleCode,
    Name,
    CalculationStartMonth,
    CalculationStartDay,
    CalculationEndMonth,
    CalculationEndDay,
    Divisor,
    PaymentDueMonth,
    PaymentDueDay,
    IncludesOrdinarySalary,
    IncludesOvertime,
    IncludesSalaryInKind,
    ExcludesCommonIllnessSubsidy,
    IncludesMaternitySubsidy,
    EffectiveFromDate,
    EffectiveToDate,
    IsActive
)
VALUES
    (N'CostaRicaPrivateSector', N'Costa Rica Private Sector Aguinaldo', 12, 1, 11, 30, 12, 12, 20, 1, 1, 1, 1, 1, '2026-01-01', NULL, 1);

UPDATE T
SET
    Name = S.Name,
    CalculationStartMonth = S.CalculationStartMonth,
    CalculationStartDay = S.CalculationStartDay,
    CalculationEndMonth = S.CalculationEndMonth,
    CalculationEndDay = S.CalculationEndDay,
    Divisor = S.Divisor,
    PaymentDueMonth = S.PaymentDueMonth,
    PaymentDueDay = S.PaymentDueDay,
    IncludesOrdinarySalary = S.IncludesOrdinarySalary,
    IncludesOvertime = S.IncludesOvertime,
    IncludesSalaryInKind = S.IncludesSalaryInKind,
    ExcludesCommonIllnessSubsidy =
        S.ExcludesCommonIllnessSubsidy,
    IncludesMaternitySubsidy = S.IncludesMaternitySubsidy,
    EffectiveToDate = S.EffectiveToDate,
    IsActive = S.IsActive,
    UpdatedAtUtc = SYSUTCDATETIME()
FROM Payroll.AguinaldoRules AS T
INNER JOIN @AguinaldoRules AS S
    ON S.AguinaldoRuleCode = T.AguinaldoRuleCode
    AND S.EffectiveFromDate = T.EffectiveFromDate
WHERE EXISTS
(
    SELECT
        S.Name,
        S.CalculationStartMonth,
        S.CalculationStartDay,
        S.CalculationEndMonth,
        S.CalculationEndDay,
        S.Divisor,
        S.PaymentDueMonth,
        S.PaymentDueDay,
        S.IncludesOrdinarySalary,
        S.IncludesOvertime,
        S.IncludesSalaryInKind,
        S.ExcludesCommonIllnessSubsidy,
        S.IncludesMaternitySubsidy,
        S.EffectiveToDate,
        S.IsActive
    EXCEPT
    SELECT
        T.Name,
        T.CalculationStartMonth,
        T.CalculationStartDay,
        T.CalculationEndMonth,
        T.CalculationEndDay,
        T.Divisor,
        T.PaymentDueMonth,
        T.PaymentDueDay,
        T.IncludesOrdinarySalary,
        T.IncludesOvertime,
        T.IncludesSalaryInKind,
        T.ExcludesCommonIllnessSubsidy,
        T.IncludesMaternitySubsidy,
        T.EffectiveToDate,
        T.IsActive
);

INSERT INTO Payroll.AguinaldoRules
(
    AguinaldoRuleCode,
    Name,
    CalculationStartMonth,
    CalculationStartDay,
    CalculationEndMonth,
    CalculationEndDay,
    Divisor,
    PaymentDueMonth,
    PaymentDueDay,
    IncludesOrdinarySalary,
    IncludesOvertime,
    IncludesSalaryInKind,
    ExcludesCommonIllnessSubsidy,
    IncludesMaternitySubsidy,
    EffectiveFromDate,
    EffectiveToDate,
    IsActive
)
SELECT
    S.AguinaldoRuleCode,
    S.Name,
    S.CalculationStartMonth,
    S.CalculationStartDay,
    S.CalculationEndMonth,
    S.CalculationEndDay,
    S.Divisor,
    S.PaymentDueMonth,
    S.PaymentDueDay,
    S.IncludesOrdinarySalary,
    S.IncludesOvertime,
    S.IncludesSalaryInKind,
    S.ExcludesCommonIllnessSubsidy,
    S.IncludesMaternitySubsidy,
    S.EffectiveFromDate,
    S.EffectiveToDate,
    S.IsActive
FROM @AguinaldoRules AS S
WHERE NOT EXISTS
(
    SELECT 1
    FROM Payroll.AguinaldoRules AS T
    WHERE
        T.AguinaldoRuleCode = S.AguinaldoRuleCode
        AND T.EffectiveFromDate = S.EffectiveFromDate
);
