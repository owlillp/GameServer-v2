namespace AuthService.Domain.Accounts;

public sealed record UserProfile
{
    public int? Age { get; init; }

    public string? Bio { get; init; }

    public string? Location { get; init; }

    public static UserProfile Empty { get; } = new ();
}