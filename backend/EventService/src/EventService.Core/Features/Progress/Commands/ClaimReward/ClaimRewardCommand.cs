using Shared.Core.Abstractions;

namespace EventService.Core.Features.Progress.Commands.ClaimReward;

public sealed record ClaimRewardCommand(
    string EventCode,
    string RewardCode,
    Guid AccountId,
    string? IdempotencyKey) : ICommand;
