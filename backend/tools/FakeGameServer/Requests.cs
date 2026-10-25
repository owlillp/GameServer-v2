namespace FakeGameServer;

public sealed record ConnectRequest(string Ticket);

public sealed record ReportRequest(string? ActivityId, int? DurationSeconds);
