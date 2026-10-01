using System.Net;
using TemplateService.IntegrationTests.Infrastructure;

namespace TemplateService.IntegrationTests.Features;

public sealed class HealthTests : IntegrationTestsBase
{
    public HealthTests(IntegrationTestsWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Health_Anonymous_ReturnsSuccess()
    {
        ClearAuthorization();

        HttpResponseMessage response = await HttpClient.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocument_Anonymous_ReturnsSuccess()
    {
        ClearAuthorization();

        HttpResponseMessage response = await HttpClient.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
