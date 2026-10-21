using Shared.Core.Abstractions;

namespace MatchmakingService.Core.Features.Parties.Queries.GetParty;

public sealed record GetPartyQuery(Guid? PartyId, string? Code) : IQuery;
