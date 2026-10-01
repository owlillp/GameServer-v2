namespace AuthService.Domain;

// Заглушка AuthService: мини-модель аккаунта, чтобы слои Domain/Infrastructure
// были готовы. Полноценные Identity/OpenIddict-сущности добавим, когда сервис
// станет настоящим issuer'ом.
public sealed class Account
{
    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    // EF Core
    private Account() { }

    public Account(string email)
    {
        Id = Guid.CreateVersion7();
        Email = email.Trim();
        CreatedAt = DateTime.UtcNow;
    }
}
