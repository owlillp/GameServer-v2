using Shared.Core.Abstractions;

namespace MatchmakingService.Core.Features.Parties.Commands.JoinParty;

public sealed record JoinPartyCommand(Guid AccountId, string Code) : ICommand;
