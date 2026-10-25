using SessionService.Domain.Reports;

namespace SessionService.Core.Abstractions;

public interface IRawReportRepository
{
    Task<RawReport?> GetByActivityAsync(string activityId, CancellationToken cancellationToken);

    Task AddAsync(RawReport report, CancellationToken cancellationToken);
}
