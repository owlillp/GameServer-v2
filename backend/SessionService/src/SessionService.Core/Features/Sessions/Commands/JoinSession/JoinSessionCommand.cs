using Shared.Core.Abstractions;

namespace SessionService.Core.Features.Sessions.Commands.JoinSession;

public sealed record JoinSessionCommand(Guid AccountId, string Region, string? Mode) : ICommand;
