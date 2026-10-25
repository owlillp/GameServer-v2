using Shared.Core.Abstractions;

namespace SessionService.Core.Features.Admin.Queries.ListServers;

public sealed record ListServersQuery(string? Region) : IQuery;
