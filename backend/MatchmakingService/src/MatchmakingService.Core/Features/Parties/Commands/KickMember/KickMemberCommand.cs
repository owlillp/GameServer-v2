using Shared.Core.Abstractions;

namespace MatchmakingService.Core.Features.Parties.Commands.KickMember;

public sealed record KickMemberCommand(Guid LeaderAccountId, Guid AccountId) : ICommand;
