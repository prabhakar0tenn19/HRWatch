namespace HRWatch.Application.Common.Interfaces;

public record CosecPunchRecord(
    string EmployeeCode,
    DateOnly PunchDate,
    TimeOnly PunchTime,
    string DeviceName,
    int EntryExitType,
    string? IndexNo);

public interface ICosecBiometricApiClient
{
    Task<IReadOnlyList<CosecPunchRecord>> GetPunchesForDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<HashSet<string>> GetPresentEmployeeCodesForDateAsync(DateOnly date, CancellationToken cancellationToken = default);
}
