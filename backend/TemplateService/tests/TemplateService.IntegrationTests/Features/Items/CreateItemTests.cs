using Shared.SharedKernel;
using TemplateService.Contracts.Items;
using TemplateService.IntegrationTests.Infrastructure;

namespace TemplateService.IntegrationTests.Features.Items;

public sealed class CreateItemTests : IntegrationTestsBase
{
    public CreateItemTests(IntegrationTestsWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateItem_Authorized_ReturnsItemOwnedByCurrentUser()
    {
        Guid userId = Guid.NewGuid();
        AuthorizeAs(userId, roles: "User");

        Envelope<ItemDto> envelope = await PostAsync<ItemDto>(
            "/items",
            new CreateItemRequest("Первая запись", "Описание"));

        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Equal("Первая запись", envelope.Result.Name);
        Assert.Equal("Описание", envelope.Result.Description);
        Assert.Equal(userId, envelope.Result.OwnerId);
    }

    [Fact]
    public async Task CreateItem_Anonymous_IsUnauthorized()
    {
        ClearAuthorization();

        HttpResponseMessage response = await PostRawAsync(
            "/items",
            new CreateItemRequest("Аноним", null));

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateItem_InvalidName_ReturnsValidationError()
    {
        AuthorizeAs(roles: "User");

        HttpResponseMessage response = await PostRawAsync(
            "/items",
            new CreateItemRequest(string.Empty, null));

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
}
