using Shared.Core.Abstractions;

namespace SessionService.Core.Features.Sessions.Queries.GetMySession;

public sealed record GetMySessionQuery(Guid AccountId) : IQuery;
