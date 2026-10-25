namespace MatchmakingService.Core.Configuration;

/// <summary>
/// Where match results come from during the migration to game servers.
/// </summary>
public enum ResultsSource
{
    /// <summary>
    /// Results are entered by an admin; matchmaking publishes match-completed.session itself.
    /// </summary>
    Admin = 1,

    /// <summary>
    /// A game server is allocated through SessionService and reports the results; SessionService publishes
    /// match-completed.session. Admin endpoints stay for disputes.
    /// </summary>
    Session = 2,
}
