namespace LithoManager.Application.Features.Payroll.Runs.Calculation;

public static class PayrollConceptCodes
{
    public const string BaseSalary = "BaseSalary";
    public const string Overtime = "Overtime";
    public const string SalaryInKind = "SalaryInKind";
    public const string Aguinaldo = "Aguinaldo";
    public const string EmployeeSocialContribution = "EmployeeSocialContribution";
    public const string IncomeTax = "IncomeTax";
    public const string EmployerSocialContribution = "EmployerSocialContribution";
    public const string DisabilitySubsidy = "DisabilitySubsidy";
}

public static class PayrollConceptKinds
{
    public const string Earning = "Earning";
    public const string Deduction = "Deduction";
    public const string EmployerContribution = "EmployerContribution";
    public const string Informational = "Informational";
}

/// <summary>
/// Raised when the configuration needed to calculate a payroll is missing
/// (for example a required payroll concept or the aguinaldo rule).
/// </summary>
public sealed class PayrollCalculationException : Exception
{
    public PayrollCalculationException(string message)
        : base(message)
    {
    }
}

internal sealed class ConceptCatalog
{
    private readonly Dictionary<string, CalculationConcept> _byCode;
    private readonly Dictionary<int, CalculationConcept> _byId;

    public ConceptCatalog(IEnumerable<CalculationConcept> concepts)
    {
        ArgumentNullException.ThrowIfNull(concepts);

        List<CalculationConcept> list = concepts.ToList();

        _byCode = list.ToDictionary(
            c => c.PayrollConceptCode,
            StringComparer.Ordinal);

        _byId = list.ToDictionary(c => c.PayrollConceptId);
    }

    public CalculationConcept Require(string code)
    {
        return _byCode.TryGetValue(code, out CalculationConcept? concept)
            ? concept
            : throw new PayrollCalculationException(
                $"The active payroll concept '{code}' is required.");
    }

    public CalculationConcept? Find(string code)
    {
        return _byCode.GetValueOrDefault(code);
    }

    public bool IsTaxable(int conceptId)
    {
        return _byId.TryGetValue(conceptId, out CalculationConcept? concept)
            && concept.IsTaxableForIncomeTax;
    }

    public bool IsSubjectToSocialContributions(int conceptId)
    {
        return _byId.TryGetValue(conceptId, out CalculationConcept? concept)
            && concept.IsSubjectToSocialContributions;
    }
}
