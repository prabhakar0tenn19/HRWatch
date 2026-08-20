using HRWatch.Domain.Enums;

namespace HRWatch.Domain.Services;

public interface IWfoEvaluationService
{
    int GetRequiredWfoDays(string? designation, bool isDeployed, string? rulesJson = null);
    
    (bool IsViolator, int Shortfall, ViolationSeverity? Severity) EvaluateWeeklyCompliance(
        int actualPresentDays, 
        int requiredDays, 
        int approvedLeaveDays = 0, 
        int approvedWfhDays = 0, 
        int exceptionDays = 0, 
        int absentDays = 0);
}
