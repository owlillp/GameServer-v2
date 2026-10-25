namespace MatchmakingService.Contracts.Requests;

public sealed record EnqueuePartyRequest(string? Mode, string? Region = null);
