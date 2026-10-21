using Shared.Core.Abstractions;

namespace MatchmakingService.Core.Features.Parties.Queries.GetMyParty;

public sealed record GetMyPartyQuery(Guid AccountId) : IQuery;
