using Shared.Core.Abstractions;

namespace SessionService.Core.Features.Admin.Queries.ListSessions;

public sealed record ListSessionsQuery(bool OpenOnly, int Limit) : IQuery;
