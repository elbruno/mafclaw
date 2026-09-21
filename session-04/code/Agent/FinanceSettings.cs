// Objective: load one connection contract for all three hosts.
// A. Read local user-secrets and environment settings.
// B. Validate required values without displaying them.
// C. Keep optional governance and telemetry explicit.
using Microsoft.Extensions.Configuration;

namespace MafClaw.Session04;

public sealed record FinanceSettings(
    Uri ProjectEndpoint, string Model, Uri? OtlpEndpoint = null,
    bool PurviewEnabled = false, string? PurviewClientId = null,
    bool FoundryMemoryEnabled = false, string? MemoryStore = null, string? MemoryScope = null)
{
    public static Uri? LoadTelemetryEndpoint()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<FinanceSettings>(optional: true).AddEnvironmentVariables().Build();
        return ParseTelemetryEndpoint(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
    }

    private static Uri? ParseTelemetryEndpoint(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme is not ("http" or "https") ||
            !string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment))
            throw new FinanceConfigurationException("OTEL_EXPORTER_OTLP_ENDPOINT must be an HTTP(S) base URI without a query or fragment.");
        return endpoint;
    }

    public static FinanceSettings Load()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<FinanceSettings>(optional: true)
            .AddEnvironmentVariables().Build();
        var endpoint = configuration["Foundry:ProjectEndpoint"] ?? configuration["FOUNDRY_PROJECT_ENDPOINT"];
        var model = configuration["Foundry:Model"] ?? configuration["FOUNDRY_MODEL"];
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new FinanceConfigurationException("Foundry:ProjectEndpoint must be configured as an HTTPS URI.");
        if (string.IsNullOrWhiteSpace(model))
            throw new FinanceConfigurationException("Foundry:Model must be configured.");
        var otlp = ParseTelemetryEndpoint(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        var enabledValue = configuration["Purview:Enabled"] ?? configuration["PURVIEW_ENABLED"];
        var enabled = false;
        if (!string.IsNullOrWhiteSpace(enabledValue) && !bool.TryParse(enabledValue, out enabled))
            throw new FinanceConfigurationException("Purview:Enabled must be true or false.");
        var clientId = configuration["Purview:ClientId"] ?? configuration["PURVIEW_CLIENT_APP_ID"];
        if (enabled && !Guid.TryParse(clientId, out _))
            throw new FinanceConfigurationException("Purview requires an explicitly configured application ID.");
        var memoryFlag = configuration["Foundry:MemoryEnabled"] ?? configuration["FOUNDRY_MEMORY_ENABLED"];
        var memoryEnabled = false;
        if (!string.IsNullOrWhiteSpace(memoryFlag) && !bool.TryParse(memoryFlag, out memoryEnabled))
            throw new FinanceConfigurationException("Foundry:MemoryEnabled must be true or false.");
        var memoryStore = configuration["Foundry:MemoryStore"] ?? configuration["FOUNDRY_MEMORY_STORE"];
        var memoryScope = configuration["Foundry:MemoryScope"] ?? configuration["FOUNDRY_MEMORY_SCOPE"];
        if (memoryEnabled && string.IsNullOrWhiteSpace(memoryStore))
            throw new FinanceConfigurationException("Managed memory requires an existing Foundry:MemoryStore.");
        if (memoryEnabled && string.IsNullOrWhiteSpace(memoryScope))
            throw new FinanceConfigurationException("Managed memory requires a unique trusted Foundry:MemoryScope for the current user.");
        if (string.Equals(configuration["OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT"], "true",
            StringComparison.OrdinalIgnoreCase))
            throw new FinanceConfigurationException("Message-content telemetry is disabled in this educational sample.");
        return new(uri, model, otlp, enabled, clientId, memoryEnabled, memoryStore, memoryScope);
    }
}
