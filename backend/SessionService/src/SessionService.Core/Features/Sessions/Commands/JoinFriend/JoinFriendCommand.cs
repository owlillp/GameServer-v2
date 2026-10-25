using Shared.Core.Abstractions;

namespace SessionService.Core.Features.Sessions.Commands.JoinFriend;

public sealed record JoinFriendCommand(Guid AccountId, Guid FriendAccountId) : ICommand;
