using Shared.Core.Abstractions;

using MatchmakingService.Contracts.Requests;

namespace MatchmakingService.Core.Features.Matches.Commands.CreateMatch;

public sealed record CreateMatchCommand(CreateMatchRequest Request) : ICommand;
