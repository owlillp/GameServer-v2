using Shared.Core.Abstractions;

using MatchmakingService.Contracts.Requests;

namespace MatchmakingService.Core.Features.Matches.Commands.SubmitResults;

public sealed record SubmitResultsCommand(Guid MatchId, IReadOnlyList<MatchResultRequest> Results) : ICommand;
