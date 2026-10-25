using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SessionService.Core.Configuration;
using SessionService.Core.GameServers;
using SessionService.Core.Services;
using SessionService.Domain;
using Xunit;

namespace SessionService.UnitTests;

public sealed class JoinTicketServiceTests
{
    [Fact]
    public async Task Issue_ThenValidate_ReturnsClaims()
    {
        using JoinTicketService tickets = Create();
        Guid account = Guid.NewGuid();
        Guid session = Guid.NewGuid();
        Guid server = Guid.NewGuid();

        JoinTicket ticket = tickets.Issue(account, session, server, DateTime.UtcNow);
        var result = await tickets.ValidateAsync(ticket.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal(account, result.Value.AccountId);
        Assert.Equal(session, result.Value.SessionId);
        Assert.Equal(server, result.Value.ServerId);
        Assert.Equal(ticket.TicketId, result.Value.TicketId);
    }

    [Fact]
    public async Task Validate_RejectsExpiredTicket()
    {
        using JoinTicketService tickets = Create();

        JoinTicket ticket = tickets.Issue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-10));
        var result = await tickets.ValidateAsync(ticket.Token);

        Assert.Equal(SessionErrors.TicketInvalid, result.Error);
    }

    [Fact]
    public async Task Validate_RejectsTicketSignedByAnotherKey()
    {
        using JoinTicketService issuer = Create();
        using JoinTicketService verifier = Create();

        JoinTicket ticket = issuer.Issue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        Assert.True((await verifier.ValidateAsync(ticket.Token)).IsFailure);
        Assert.True((await verifier.ValidateAsync("not-a-jwt")).IsFailure);
    }

    [Fact]
    public void ConfiguredKey_IsUsed_AndPublicKeyIsExported()
    {
        using var rsa = RSA.Create(2048);
        string pem = rsa.ExportPkcs8PrivateKeyPem();

        using JoinTicketService tickets = Create(pem);

        Assert.Equal(rsa.ExportSubjectPublicKeyInfoPem(), tickets.PublicKeyPem);
    }

    [Fact]
    public void MissingKey_WithoutEphemeralFlag_Throws()
    {
        var options = Options.Create(new JoinTicketOptions { AllowEphemeralKey = false });

        Assert.Throws<InvalidOperationException>(() =>
            new JoinTicketService(options, NullLogger<JoinTicketService>.Instance));
    }

    [Fact]
    public void DeriveMatchId_IsStable()
    {
        Guid guidActivity = Guid.NewGuid();

        Assert.Equal(guidActivity, GameServerOperations.DeriveMatchId(guidActivity.ToString()));
        Assert.Equal(
            GameServerOperations.DeriveMatchId("race-42"),
            GameServerOperations.DeriveMatchId("race-42"));
        Assert.NotEqual(
            GameServerOperations.DeriveMatchId("race-42"),
            GameServerOperations.DeriveMatchId("race-43"));
    }

    private static JoinTicketService Create(string pem = "") =>
        new(
            Options.Create(new JoinTicketOptions { SigningKeyPem = pem, AllowEphemeralKey = true }),
            NullLogger<JoinTicketService>.Instance);
}
