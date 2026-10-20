using System.Text.Json;

namespace RealtimeService.Core.Messaging;

internal static class RealtimeJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static JsonElement ToElement<T>(T payload) =>
        JsonSerializer.SerializeToElement(payload, Options);
}
