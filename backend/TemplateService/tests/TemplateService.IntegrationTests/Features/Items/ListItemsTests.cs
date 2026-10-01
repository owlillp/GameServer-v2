using Shared.SharedKernel;
using Shared.SharedKernel.Responses;
using TemplateService.Contracts.Items;
using TemplateService.IntegrationTests.Infrastructure;

namespace TemplateService.IntegrationTests.Features.Items;

public sealed class ListItemsTests : IntegrationTestsBase
{
    public ListItemsTests(IntegrationTestsWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ListItems_ReturnsCreatedItems()
    {
        AuthorizeAs(roles: "User");

        await PostAsync<ItemDto>("/items", new CreateItemRequest("Alpha", null));
        await PostAsync<ItemDto>("/items", new CreateItemRequest("Beta", null));

        Envelope<PaginationResponse<ItemDto>> envelope =
            await GetAsync<PaginationResponse<ItemDto>>("/items?page=1&pageSize=10");

        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Equal(2, envelope.Result.TotalCount);
        Assert.Equal(2, envelope.Result.Items.Count);
    }

    [Fact]
    public async Task ListItems_Search_FiltersByName()
    {
        AuthorizeAs(roles: "User");

        await PostAsync<ItemDto>("/items", new CreateItemRequest("Alpha", null));
        await PostAsync<ItemDto>("/items", new CreateItemRequest("Beta", null));

        Envelope<PaginationResponse<ItemDto>> envelope =
            await GetAsync<PaginationResponse<ItemDto>>("/items?page=1&pageSize=10&search=alph");

        Assert.NotNull(envelope.Result);
        Assert.Equal(1, envelope.Result.TotalCount);

        ItemDto item = Assert.Single(envelope.Result.Items);
        Assert.Equal("Alpha", item.Name);
    }

    [Fact]
    public async Task ListItems_Anonymous_IsUnauthorized()
    {
        ClearAuthorization();

        HttpResponseMessage response = await HttpClient.GetAsync("/items");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
