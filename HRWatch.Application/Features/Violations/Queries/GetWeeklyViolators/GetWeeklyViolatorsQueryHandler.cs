using HRWatch.Application.Common.Abstractions;

namespace HRWatch.Application.Features.Violations.Queries.GetWeeklyViolators;

public class GetWeeklyViolatorsQueryHandler : IQueryHandler<GetWeeklyViolatorsQuery, IReadOnlyList<WeeklyViolatorDto>>
{
    private readonly IViolationRepository _violationRepository;

    public GetWeeklyViolatorsQueryHandler(IViolationRepository violationRepository)
    {
        _violationRepository = violationRepository;
    }

    public async Task<Result<IReadOnlyList<WeeklyViolatorDto>>> HandleAsync(
        GetWeeklyViolatorsQuery query,
        CancellationToken cancellationToken = default)
    {
        var violations = await _violationRepository.GetAllAsync(cancellationToken);

        var filtered = violations.AsEnumerable();

        if (query.WeekStartDate.HasValue)
        {
            filtered = filtered.Where(v => v.OccurredOn.Date == query.WeekStartDate.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            filtered = filtered.Where(v => string.Equals(v.Employee?.Department, query.Department, StringComparison.OrdinalIgnoreCase));
        }

        var result = filtered.Select(v => new WeeklyViolatorDto(
            v.Id,
            v.EmployeeId,
            v.Employee?.FullName ?? "Unknown",
            v.Employee?.Email ?? string.Empty,
            v.Employee?.Designation?.Title ?? "N/A",
            v.Employee?.Department ?? "General",
            v.OccurredOn,
            v.Type.ToString(),
            v.Severity.ToString(),
            v.Description,
            v.IsAcknowledged
        )).ToList();

        return Result<IReadOnlyList<WeeklyViolatorDto>>.Success(result);
    }
}
