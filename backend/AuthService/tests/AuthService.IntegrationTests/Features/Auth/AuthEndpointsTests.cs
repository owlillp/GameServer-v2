using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuthService.Contracts.Dtos;
using AuthService.Contracts.Requests;
using AuthService.IntegrationTests.Infrastructure;
using Shared.SharedKernel;

namespace AuthService.IntegrationTests.Features.Auth;

public sealed class AuthEndpointsTests : IntegrationTestsBase
{
    private const string EMAIL = "auth-endpoints@test.com";
    private const string PASSWORD = "TestPass123!";
    private const string USER_NAME = "authendpoints";
    private const string SCOPE = "openid profile email offline_access auth";

    private readonly OidcTestHelper _oidc;

    public AuthEndpointsTests(IntegrationTestsWebFactory factory)
        : base(factory)
    {
        _oidc = new OidcTestHelper(factory);
    }

    [Fact]
    public async Task Register_WithValidRequest_CreatesAccount()
    {
        await SeedPlatformConfigAsync();

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest(EMAIL, USER_NAME, PASSWORD));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Guid accountId = await GetAccountIdAsync(EMAIL);
        Assert.NotEqual(Guid.Empty, accountId);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        await SeedPlatformConfigAsync();
        await RegisterAsync(EMAIL, PASSWORD, USER_NAME);

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest(EMAIL, "anotheruser", PASSWORD));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        await SeedPlatformConfigAsync();

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest("not-an-email", USER_NAME, PASSWORD));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccess()
    {
        await SeedPlatformConfigAsync();
        await RegisterAsync(EMAIL, PASSWORD, USER_NAME);

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(EMAIL, PASSWORD));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await SeedPlatformConfigAsync();
        await RegisterAsync(EMAIL, PASSWORD, USER_NAME);

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(EMAIL, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_IsUnauthorized()
    {
        HttpResponseMessage response = await HttpClient.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithUserToken_ReturnsProfile()
    {
        await SeedPlatformConfigAsync();
        await RegisterAsync(EMAIL, PASSWORD, USER_NAME);
        await _oidc.LoginAsync(EMAIL, PASSWORD);

        OidcTokenResponse tokens = await _oidc.ExecuteAuthorizationCodeFlowAsync(
            IntegrationTestsWebFactory.WEB_CLIENT_ID,
            clientSecret: null,
            SCOPE);

        Assert.True(tokens.IsSuccess, tokens.RawResponse);

        using HttpClient client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        HttpResponseMessage response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Envelope<ProfileDto>? envelope = await response.Content.ReadFromJsonAsync<Envelope<ProfileDto>>();

        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Equal(EMAIL, envelope.Result.Email);
        Assert.Equal(USER_NAME, envelope.Result.Username);
    }
}
