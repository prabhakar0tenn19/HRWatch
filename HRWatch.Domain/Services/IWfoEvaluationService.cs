using HRWatch.Domain.Enums;

namespace HRWatch.Domain.Services;

public interface IWfoEvaluationService
{
    int GetRequiredWfoDays(string? designation, bool isDeployed, string? rulesJson = null);
    (bool IsViolator, int Shortfall, ViolationSeverity? Severity) EvaluateWeeklyCompliance(int actualPresentDays, int requiredDays);
}
