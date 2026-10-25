using System.Security.Claims;
using System.Security.Cryptography;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SessionService.Core.Configuration;
using SessionService.Domain;
using Shared.SharedKernel.Errors;

namespace SessionService.Core.Services;

public sealed record JoinTicket(string Token, string TicketId, DateTime ExpiresAt);

public sealed record JoinTicketClaims(Guid AccountId, Guid SessionId, Guid ServerId, string TicketId, DateTime ExpiresAt);

/// <summary>
/// Short-lived RS256 JWT that lets one player into one session on one server.
/// The private key never leaves SessionService; servers get the public key on registration.
/// </summary>
public sealed class JoinTicketService : IDisposable
{
    private const string SESSION_CLAIM = "session_id";
    private const string SERVER_CLAIM = "server_id";

    private readonly RSA _rsa;
    private readonly RsaSecurityKey _key;
    private readonly SigningCredentials _credentials;
    private readonly JsonWebTokenHandler _handler = new();
    private readonly JoinTicketOptions _options;

    public JoinTicketService(IOptions<JoinTicketOptions> options, ILogger<JoinTicketService> logger)
    {
        _options = options.Value;
        _rsa = RSA.Create();

        if (!string.IsNullOrWhiteSpace(_options.SigningKeyPem))
        {
            _rsa.ImportFromPem(_options.SigningKeyPem);
        }
        else if (_options.AllowEphemeralKey)
        {
            _rsa.KeySize = 2048;
            logger.LogWarning("JoinTickets:SigningKeyPem is not set, using an ephemeral key (dev only)");
        }
        else
        {
            _rsa.Dispose();
            throw new InvalidOperationException("JoinTickets:SigningKeyPem is not configured");
        }

        _key = new RsaSecurityKey(_rsa) { KeyId = _options.KeyId };
        _credentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256);
        PublicKeyPem = _rsa.ExportSubjectPublicKeyInfoPem();
    }

    public string PublicKeyPem { get; }

    public string KeyId => _options.KeyId;

    public TimeSpan Lifetime => TimeSpan.FromSeconds(_options.LifetimeSeconds);

    public JoinTicket Issue(Guid accountId, Guid sessionId, Guid serverId, DateTime now)
    {
        string ticketId = Guid.CreateVersion7().ToString("N");
        DateTime expiresAt = now.Add(Lifetime);

        string token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = _credentials,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, accountId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, ticketId),
                new Claim(SESSION_CLAIM, sessionId.ToString()),
                new Claim(SERVER_CLAIM, serverId.ToString()),
            ]),
        });

        return new JoinTicket(token, ticketId, expiresAt);
    }

    public async Task<Result<JoinTicketClaims, Error>> ValidateAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return SessionErrors.TicketInvalid;
        }

        TokenValidationResult result = await _handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = _key,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ClockSkew = TimeSpan.FromSeconds(5),
        });

        if (!result.IsValid
            || !TryGetGuid(result, JwtRegisteredClaimNames.Sub, out Guid accountId)
            || !TryGetGuid(result, SESSION_CLAIM, out Guid sessionId)
            || !TryGetGuid(result, SERVER_CLAIM, out Guid serverId)
            || !TryGetString(result, JwtRegisteredClaimNames.Jti, out string ticketId)
            || result.SecurityToken is not JsonWebToken jwt)
        {
            return SessionErrors.TicketInvalid;
        }

        return new JoinTicketClaims(accountId, sessionId, serverId, ticketId, jwt.ValidTo);
    }

    public void Dispose() => _rsa.Dispose();

    private static bool TryGetGuid(TokenValidationResult result, string claim, out Guid value)
    {
        value = Guid.Empty;
        return TryGetString(result, claim, out string raw) && Guid.TryParse(raw, out value);
    }

    private static bool TryGetString(TokenValidationResult result, string claim, out string value)
    {
        if (result.Claims.TryGetValue(claim, out object? raw) && raw is string text && text.Length > 0)
        {
            value = text;
            return true;
        }

        value = string.Empty;
        return false;
    }
}
