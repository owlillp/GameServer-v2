using Shared.Core.Abstractions;

namespace MatchmakingService.Core.Features.Queues.Queries.ListQueue;

public sealed record ListQueueQuery(int Limit) : IQuery;
