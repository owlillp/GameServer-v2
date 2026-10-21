using Shared.Core.Abstractions;

namespace MatchmakingService.Core.Features.Parties.Commands.LeaveParty;

public sealed record LeavePartyCommand(Guid AccountId) : ICommand;
