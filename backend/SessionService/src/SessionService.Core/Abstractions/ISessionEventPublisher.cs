using SessionService.Domain.Reports;
using SessionService.Domain.Sessions;

namespace SessionService.Core.Abstractions;

public interface ISessionEventPublisher
{
    Task PublishPlayerJoinedAsync(Session session, Guid accountId, DateTime joinedAt);

    Task PublishPlayerLeftAsync(Session session, Guid accountId, DateTime leftAt);

    Task PublishSessionClosedAsync(Session session);

    Task PublishMatchCompletedAsync(Session session, RawReport report, MatchReport data);
}
