using Microsoft.EntityFrameworkCore;
using SessionService.Core.Abstractions;
using SessionService.Domain.Reports;

namespace SessionService.Infrastructure.Postgres.Repositories;

public sealed class RawReportRepository(SessionServiceDbContext dbContext) : IRawReportRepository
{
    public Task<RawReport?> GetByActivityAsync(string activityId, CancellationToken cancellationToken) =>
        dbContext.RawReports
            .AsNoTracking()
            .FirstOrDefaultAsync(report => report.ActivityId == activityId, cancellationToken);

    public Task AddAsync(RawReport report, CancellationToken cancellationToken) =>
        dbContext.RawReports.AddAsync(report, cancellationToken).AsTask();
}
