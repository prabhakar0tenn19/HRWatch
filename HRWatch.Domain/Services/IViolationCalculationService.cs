using HRWatch.Domain.Entities;

namespace HRWatch.Domain.Services;

public interface IViolationCalculationService
{
    Violation? CalculateWfoViolation(Employee employee, WeeklyAttendance attendance, Policy policy);
}
