namespace Shared.IntegrationEvents.Players.Events;

public record PlayerProfileUpdatedEvent(Guid AccountId, string Nickname);
