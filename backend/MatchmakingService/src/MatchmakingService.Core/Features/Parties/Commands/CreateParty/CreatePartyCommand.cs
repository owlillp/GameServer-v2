using Shared.Core.Abstractions;

namespace MatchmakingService.Core.Features.Parties.Commands.CreateParty;

public sealed record CreatePartyCommand(Guid AccountId) : ICommand;
