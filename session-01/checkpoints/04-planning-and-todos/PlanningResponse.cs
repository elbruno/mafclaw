using System.Text.Json.Serialization;

namespace MafClaw.Checkpoint04;

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
    public static bool CanExecute(PlanningResponse response, bool isApproved) =>
        response.Type == PlanningResponseType.Approval && isApproved;
}
