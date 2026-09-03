using System.Reflection;
using Microsoft.Extensions.Configuration;

internal sealed record FoundrySettings(string ProjectEndpoint, string Model);

internal sealed class FoundryConfigurationException(string message)
    : InvalidOperationException(message);

internal static class FoundryConfiguration
{
    public static FoundrySettings Resolve()
    {
        var builder = new ConfigurationBuilder();

        foreach (var path in new[]
        {
            Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")
        }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            builder.AddJsonFile(path, optional: true, reloadOnChange: false);
        }

        if (!IsProduction())
        {
            builder.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);
        }
        var configuration = builder.Build();

        var endpoint = FirstNonEmpty(
            configuration["Foundry:ProjectEndpoint"],
            Environment.GetEnvironmentVariable("Foundry__ProjectEndpoint"),
            Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT"));

        var model = FirstNonEmpty(
            configuration["Foundry:Model"],
            Environment.GetEnvironmentVariable("Foundry__Model"),
            Environment.GetEnvironmentVariable("FOUNDRY_MODEL"));

        if (endpoint is null)
        {
            throw new FoundryConfigurationException(
                "Missing Foundry:ProjectEndpoint. Run configure-user-secrets.ps1 -Session 1.");
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri) ||
            endpointUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new FoundryConfigurationException(
                "Foundry:ProjectEndpoint must be an absolute HTTPS URI.");
        }

        if (model is null)
        {
            throw new FoundryConfigurationException(
                "Missing Foundry:Model. Run configure-user-secrets.ps1 -Session 1.");
        }

        return new FoundrySettings(endpoint, model);
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static bool IsProduction()
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
                          Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        return string.Equals(
            environment,
            "Production",
            StringComparison.OrdinalIgnoreCase);
    }
}
