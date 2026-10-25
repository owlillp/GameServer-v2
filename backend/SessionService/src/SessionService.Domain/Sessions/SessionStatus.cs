namespace SessionService.Domain.Sessions;

/// <summary>
/// Allocating → Active → Ending → Closed. A static server pool skips Allocating;
/// a real fleet allocator keeps the session there until the server is ready.
/// </summary>
public enum SessionStatus
{
    Allocating = 1,
    Active = 2,
    Ending = 3,
    Closed = 4,
}
