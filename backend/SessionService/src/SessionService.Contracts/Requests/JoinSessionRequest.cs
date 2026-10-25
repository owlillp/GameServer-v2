namespace SessionService.Contracts.Requests;

public sealed record JoinSessionRequest(string Region, string? Mode);
