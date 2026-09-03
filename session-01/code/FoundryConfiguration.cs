using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace MafClaw.Session01;

internal sealed record FoundrySettings(string ProjectEndpoint, string Model);

internal sealed class ConfigurationValidationException(string message) : InvalidOperationException(message);

internal static class FoundryConfiguration
{
    private const string EndpointKey = "Foundry:ProjectEndpoint";
    private const string ModelKey = "Foundry:Model";
    private const string EndpointAlias = "FOUNDRY_PROJECT_ENDPOINT";
    private const string ModelAlias = "FOUNDRY_MODEL";
    private const string EndpointCanonicalEnvironmentKey = "Foundry__ProjectEndpoint";
    private const string ModelCanonicalEnvironmentKey = "Foundry__Model";

    /// <summary>
    /// Production entry point. Effective high-to-low precedence is user-secrets,
    /// working-directory JSON, output-directory JSON, canonical environment
    /// variables, then environment aliases.
    /// </summary>
    public static FoundrySettings Resolve()
    {
        var configuration = BuildConfiguration();
        return Resolve(configuration, Environment.GetEnvironmentVariable);
    }

    /// <summary>
    /// Testable overload: supply any <see cref="IConfiguration"/> (e.g. built with
    /// <c>AddInMemoryCollection</c>) to validate config-path resolution without
    /// touching the real user-secrets store.
    /// </summary>
    internal static FoundrySettings Resolve(
        IConfiguration configuration,
        Func<string, string?> environmentLookup)
    {
        var configValues = ExtractFoundryValues(configuration);
        return Resolve(configValues, environmentLookup);
    }

    /// <summary>
    /// Lowest-level testable overload: supply a pre-built key/value dictionary
    /// and an env-var lookup delegate.
    /// </summary>
    internal static FoundrySettings Resolve(
        IReadOnlyDictionary<string, string?> configValues,
        Func<string, string?> environmentLookup)
    {
        var endpoint = ResolveEndpoint(configValues, environmentLookup);
        var model = ResolveModel(configValues, environmentLookup);

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri) ||
            !string.Equals(endpointUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConfigurationValidationException(
                $"'{EndpointKey}' must be an absolute HTTPS URI.");
        }

        return new FoundrySettings(endpoint, model);
    }

    private static string ResolveEndpoint(
        IReadOnlyDictionary<string, string?> configValues,
        Func<string, string?> environmentLookup)
    {
        var endpoint = ReadNonEmpty(configValues, EndpointKey) ??
                       ReadNonEmpty(environmentLookup, EndpointCanonicalEnvironmentKey);

        if (endpoint is null)
        {
            endpoint = ReadNonEmpty(environmentLookup, EndpointAlias);
        }

        if (endpoint is null)
        {
            throw new ConfigurationValidationException(
                $"""
                Missing required setting '{EndpointKey}'.
                Set it with:
                  dotnet user-secrets set "{EndpointKey}" "https://<your-project>.services.ai.azure.com/api/projects/<your-project>"
                or set the environment alias:
                  $env:{EndpointAlias} = "https://<your-project>.services.ai.azure.com/api/projects/<your-project>"
                """);
        }

        return endpoint;
    }

    private static string ResolveModel(
        IReadOnlyDictionary<string, string?> configValues,
        Func<string, string?> environmentLookup)
    {
        var model = ReadNonEmpty(configValues, ModelKey) ??
                    ReadNonEmpty(environmentLookup, ModelCanonicalEnvironmentKey);

        if (model is null)
        {
            model = ReadNonEmpty(environmentLookup, ModelAlias);
        }

        if (model is null)
        {
            throw new ConfigurationValidationException(
                $"""
                Missing required setting '{ModelKey}'.
                Set it with:
                  dotnet user-secrets set "{ModelKey}" "gpt-5.4"
                or set the environment alias:
                  $env:{ModelAlias} = "gpt-5.4"
                """);
        }

        return model;
    }

    private static string? ReadNonEmpty(
        IReadOnlyDictionary<string, string?> values,
        string key)
    {
        return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    private static string? ReadNonEmpty(
        Func<string, string?> environmentLookup,
        string key)
    {
        var value = environmentLookup(key);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>
    /// Builds the JSON and user-secrets configuration stack for local/development
    /// execution. Providers are added from lowest to highest priority:
    /// output-directory JSON, working-directory JSON, then user-secrets.
    /// Environment variables are resolved separately as lower-priority fallbacks.
    /// </summary>
    private static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder();

        // appsettings.json — optional; user-secrets or env vars suffice when absent.
        foreach (var path in new[]
        {
            Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")
        }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            builder.AddJsonFile(path, optional: true, reloadOnChange: false);
        }

        // .NET user-secrets — active in Development (the default) and all non-Production envs.
        var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
                  Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                  "Development";

        if (!string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase))
        {
            // UserSecretsId is embedded in this assembly via <UserSecretsId> in the .csproj.
            // optional: true → silent no-op when the user hasn't run configure-user-secrets.ps1 yet.
            builder.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);
        }

        return builder.Build();
    }

    private static IReadOnlyDictionary<string, string?> ExtractFoundryValues(IConfiguration configuration)
    {
        var section = configuration.GetSection("Foundry");
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [EndpointKey] = section["ProjectEndpoint"],
            [ModelKey] = section["Model"]
        };
    }
}
