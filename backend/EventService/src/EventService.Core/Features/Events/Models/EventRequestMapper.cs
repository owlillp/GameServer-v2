using System.Globalization;
using CSharpFunctionalExtensions;
using EventService.Contracts.Requests;
using EventService.Domain;
using EventService.Domain.Events;
using Shared.SharedKernel.Errors;

namespace EventService.Core.Features.Events.Models;

internal static class EventRequestMapper
{
    public static Result<EventKind, Error> ParseKind(string value) =>
        ParseEnum<EventKind>(value, EventErrors.KindInvalid);

    public static Result<IReadOnlyList<EventQuestData>, Error> MapQuests(
        IReadOnlyList<EventQuestRequest> requests)
    {
        var result = new List<EventQuestData>(requests.Count);

        foreach (EventQuestRequest request in requests)
        {
            var kindResult = ParseEnum<EventQuestKind>(request.Kind, EventErrors.QuestKindInvalid);
            if (kindResult.IsFailure)
            {
                return kindResult.Error;
            }

            var metricResult = ParseEnum<EventQuestMetric>(request.Metric, EventErrors.QuestMetricInvalid);
            if (metricResult.IsFailure)
            {
                return metricResult.Error;
            }

            result.Add(new EventQuestData(
                request.Code,
                request.DisplayName,
                request.Description,
                kindResult.Value,
                metricResult.Value,
                request.Target,
                request.Points,
                request.SortOrder));
        }

        return result;
    }

    public static IReadOnlyList<EventRewardData> MapRewards(IReadOnlyList<EventRewardRequest> requests) =>
        [.. requests.Select(request => new EventRewardData(
            request.Code,
            request.DisplayName,
            request.Description,
            request.PointsCost,
            request.CatalogItemId,
            request.ItemCode,
            request.ItemKind,
            request.Slot,
            request.Quantity,
            request.SortOrder,
            request.IsRepeatable))];

    private static Result<TEnum, Error> ParseEnum<TEnum>(string value, Error error)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)
            || int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
            || !Enum.TryParse(value, ignoreCase: true, out TEnum parsed)
            || !Enum.IsDefined(parsed))
        {
            return error;
        }

        return parsed;
    }
}
