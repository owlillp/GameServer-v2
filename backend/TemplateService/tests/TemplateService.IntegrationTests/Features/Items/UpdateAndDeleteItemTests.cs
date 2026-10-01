using Microsoft.EntityFrameworkCore;
using Shared.SharedKernel;
using TemplateService.Contracts.Items;
using TemplateService.IntegrationTests.Infrastructure;

namespace TemplateService.IntegrationTests.Features.Items;

public sealed class UpdateAndDeleteItemTests : IntegrationTestsBase
{
    public UpdateAndDeleteItemTests(IntegrationTestsWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task UpdateItem_AsOwner_UpdatesNameAndDescription()
    {
        Guid userId = Guid.NewGuid();
        AuthorizeAs(userId, roles: "User");

        Envelope<ItemDto> created = await PostAsync<ItemDto>(
            "/items",
            new CreateItemRequest("Старое имя", "Старое описание"));

        Assert.NotNull(created.Result);

        Envelope<ItemDto> updated = await PutAsync<ItemDto>(
            $"/items/{created.Result.Id}",
            new UpdateItemRequest("Новое имя", "Новое описание"));

        Assert.NotNull(updated.Result);
        Assert.Equal("Новое имя", updated.Result.Name);
        Assert.Equal("Новое описание", updated.Result.Description);
    }

    [Fact]
    public async Task DeleteItem_AsOwner_RemovesItem()
    {
        Guid userId = Guid.NewGuid();
        AuthorizeAs(userId, roles: "User");

        Envelope<ItemDto> created = await PostAsync<ItemDto>(
            "/items",
            new CreateItemRequest("На удаление", null));

        Assert.NotNull(created.Result);

        HttpResponseMessage response = await HttpClient.DeleteAsync($"/items/{created.Result.Id}");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        bool exists = await ExecuteInDbAsync(db =>
            db.Items.AnyAsync(item => item.Id == created.Result.Id));

        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteItem_AsOtherUserWithoutManagePermission_IsForbidden()
    {
        AuthorizeAs(roles: "User");

        Envelope<ItemDto> created = await PostAsync<ItemDto>(
            "/items",
            new CreateItemRequest("Чужая запись", null));

        Assert.NotNull(created.Result);

        AuthorizeAs(roles: "User");

        HttpResponseMessage response = await HttpClient.DeleteAsync($"/items/{created.Result.Id}");

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteItem_AsModerator_RemovesItemOfAnotherUser()
    {
        AuthorizeAs(roles: "User");

        Envelope<ItemDto> created = await PostAsync<ItemDto>(
            "/items",
            new CreateItemRequest("Запись игрока", null));

        Assert.NotNull(created.Result);

        AuthorizeAs(roles: "Moderator");

        HttpResponseMessage response = await HttpClient.DeleteAsync($"/items/{created.Result.Id}");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        bool exists = await ExecuteInDbAsync(db =>
            db.Items.AnyAsync(item => item.Id == created.Result.Id));

        Assert.False(exists);
    }
}
