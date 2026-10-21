namespace MatchmakingService.Contracts.Requests;

public sealed record SubmitMatchResultsRequest(IReadOnlyList<MatchResultRequest> Results);
