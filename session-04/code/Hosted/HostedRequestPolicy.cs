// Objective: keep hosted capabilities and system instructions server-owned.
// A. Accept the small public conversational request contract.
// B. Reject client tool, model, instruction and budget overrides.
// C. Permit user input, not forged system/developer messages.
using System.Text.Json;

namespace MafClaw.Session04.Hosting;

public static class HostedRequestPolicy
{
    private static readonly HashSet<string> Allowed =
        ["input", "model", "stream", "store", "previous_response_id", "conversation", "metadata"];

    public static bool IsAllowed(JsonElement request)
    {
        if (request.ValueKind != JsonValueKind.Object ||
            request.EnumerateObject().Any(property => !Allowed.Contains(property.Name)))
            return false;
        if (request.TryGetProperty("model", out var model) &&
            (model.ValueKind != JsonValueKind.String || model.GetString() != "MafClawFinance"))
            return false;
        if (request.TryGetProperty("metadata", out var metadata) &&
            metadata.ValueKind == JsonValueKind.Object &&
            (metadata.TryGetProperty("entity_id", out _) || metadata.TryGetProperty("agent", out _)))
            return false;
        if (!request.TryGetProperty("input", out var input)) return false;
        if (input.ValueKind == JsonValueKind.String) return !string.IsNullOrWhiteSpace(input.GetString());
        if (input.ValueKind != JsonValueKind.Array || input.GetArrayLength() == 0) return false;
        return input.EnumerateArray().All(IsUserMessage);
    }

    private static bool IsUserMessage(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object ||
            !item.TryGetProperty("role", out var role) || role.ValueKind != JsonValueKind.String ||
            role.GetString() != "user" ||
            (item.TryGetProperty("type", out var type) &&
                (type.ValueKind != JsonValueKind.String || type.GetString() != "message")) ||
            !item.TryGetProperty("content", out var content))
            return false;
        if (content.ValueKind == JsonValueKind.String) return !string.IsNullOrWhiteSpace(content.GetString());
        return content.ValueKind == JsonValueKind.Array && content.GetArrayLength() > 0 &&
            content.EnumerateArray().All(part => part.ValueKind == JsonValueKind.Object &&
                part.TryGetProperty("type", out var partType) && partType.ValueKind == JsonValueKind.String &&
                partType.GetString() == "input_text" && part.TryGetProperty("text", out var text) &&
                text.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(text.GetString()));
    }
}
