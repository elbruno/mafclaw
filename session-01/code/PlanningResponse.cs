using System.Text.Json.Serialization;

namespace MafClaw.Session01;

[JsonConverter(typeof(JsonStringEnumConverter<PlanningResponseType>))]
internal enum PlanningResponseType
{
    [JsonStringEnumMemberName("clarification")]
    Clarification,
    [JsonStringEnumMemberName("approval")]
    Approval
}

internal sealed class PlanningResponse
{
    [JsonPropertyName("type")]
    public required PlanningResponseType Type { get; init; }

    [JsonPropertyName("questions")]
    public required List<PlanningQuestion> Questions { get; init; }
}

internal sealed class PlanningQuestion
{
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("choices")]
    public List<string>? Choices { get; init; }
}

internal static class PlanApprovalGate
{
    public static bool CanExecute(PlanningResponse response, bool isApproved)
    {
        return response.Type == PlanningResponseType.Approval && isApproved;
    }

    public static bool TryExecute(PlanningResponse response, bool isApproved, Action executeAction)
    {
        if (!CanExecute(response, isApproved))
        {
            return false;
        }

        executeAction();
        return true;
    }
}
