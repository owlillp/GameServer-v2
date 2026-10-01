using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Authentication.Authentication;

namespace TemplateService.IntegrationTests.Infrastructure;

/// <summary>
/// Тестовая схема авторизации: Authorization: "TestAuth {sub}|{name}|{email}|{roles}".
/// Позволяет проверять endpoints без реального AuthService.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestAuth";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string header = Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(header)
            || !header.StartsWith($"{SchemeName} ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string[] parts = header[(SchemeName.Length + 1)..].Split('|');
        if (parts.Length < 4)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid TestAuth payload."));
        }

        List<System.Security.Claims.Claim> claims =
        [
            new(AuthClaimTypes.SUB, parts[0]),
            new(AuthClaimTypes.NAME, parts[1]),
            new(AuthClaimTypes.EMAIL, parts[2]),
        ];

        foreach (string role in parts[3].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new System.Security.Claims.Claim(AuthClaimTypes.ROLE, role));
        }

        var identity = new System.Security.Claims.ClaimsIdentity(
            claims,
            SchemeName,
            AuthClaimTypes.NAME,
            AuthClaimTypes.ROLE);

        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new System.Security.Claims.ClaimsPrincipal(identity), SchemeName)));
    }
}
