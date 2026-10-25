using SessionService.Domain.Reports;
using SessionService.Domain.Servers;
using SessionService.Domain.Sessions;

namespace SessionService.Core.Abstractions;

public interface ISessionEventPublisher
{
    Task PublishPlayerJoinedAsync(Session session, Guid accountId, DateTime joinedAt);

    Task PublishPlayerLeftAsync(Session session, Guid accountId, DateTime leftAt);

    Task PublishSessionClosedAsync(Session session);

    Task PublishMatchCompletedAsync(Session session, RawReport report, MatchReport data);

    Task PublishMatchAllocatedAsync(Session session, GameServer server, DateTime allocatedAt);

    Task PublishMatchAllocationFailedAsync(Guid matchId, string reason, DateTime failedAt);
}
