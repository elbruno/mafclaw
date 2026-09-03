using System.Text;
using Azure;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace MafClaw.Session01.Tests;

public sealed class StockToolsTests
{
    [Fact]
    public void GettingKnownSymbolReturnsQuote()
    {
        var tools = CreateStockTools();

        var quote = tools.GetStockPrice("MSFT");

        Assert.Equal("MSFT", quote.Symbol);
        Assert.Equal(512.34m, quote.Price);
        Assert.Equal("USD", quote.Currency);
    }

    [Fact]
    public void GettingLowercaseSymbolNormalizesInput()
    {
        var tools = CreateStockTools();

        var quote = tools.GetStockPrice("nvda");

        Assert.Equal("NVDA", quote.Symbol);
        Assert.Equal(184.72m, quote.Price);
    }

    [Fact]
    public void GettingUnknownSymbolThrows()
    {
        var tools = CreateStockTools();

        var ex = Assert.Throws<KeyNotFoundException>(() => tools.GetStockPrice("TSLA"));

        Assert.Contains("TSLA", ex.Message);
    }

    [Fact]
    public void GettingBlankSymbolThrows()
    {
        var tools = CreateStockTools();

        Assert.Throws<ArgumentException>(() => tools.GetStockPrice("   "));
    }

    [Fact]
    public void GettingMalformedSymbolThrows()
    {
        var tools = CreateStockTools();

        Assert.Throws<ArgumentException>(() => tools.GetStockPrice("MSFT!"));
    }

    private static StockTools CreateStockTools()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "mock-market-data.json");
        return new StockTools(path);
    }
}

public sealed class FoundryConfigurationTests
{
    [Fact]
    public void CanonicalKeysWinOverAliases()
    {
        var config = new Dictionary<string, string?>
        {
            ["Foundry:ProjectEndpoint"] = "https://canonical.services.ai.azure.com/api/projects/demo",
            ["Foundry:Model"] = "gpt-5.4"
        };

        var env = new Dictionary<string, string?>
        {
            ["FOUNDRY_PROJECT_ENDPOINT"] = "https://alias.services.ai.azure.com/api/projects/alias",
            ["FOUNDRY_MODEL"] = "alias-model"
        };

        var settings = FoundryConfiguration.Resolve(config, key => env.TryGetValue(key, out var value) ? value : null);

        Assert.Equal("https://canonical.services.ai.azure.com/api/projects/demo", settings.ProjectEndpoint);
        Assert.Equal("gpt-5.4", settings.Model);
    }

    [Fact]
    public void CanonicalEnvironmentVariablesWinOverAliases()
    {
        var config = new Dictionary<string, string?>();
        var env = new Dictionary<string, string?>
        {
            ["Foundry__ProjectEndpoint"] = "https://canonical-env.services.ai.azure.com/api/projects/demo",
            ["Foundry__Model"] = "canonical-env-model",
            ["FOUNDRY_PROJECT_ENDPOINT"] = "https://alias.services.ai.azure.com/api/projects/alias",
            ["FOUNDRY_MODEL"] = "alias-model"
        };

        var settings = FoundryConfiguration.Resolve(
            config,
            key => env.TryGetValue(key, out var value) ? value : null);

        Assert.Equal(
            "https://canonical-env.services.ai.azure.com/api/projects/demo",
            settings.ProjectEndpoint);
        Assert.Equal("canonical-env-model", settings.Model);
    }

    [Fact]
    public void MissingEndpointThrowsActionableError()
    {
        var config = new Dictionary<string, string?>
        {
            ["Foundry:Model"] = "gpt-5.4"
        };

        var ex = Assert.Throws<ConfigurationValidationException>(
            () => FoundryConfiguration.Resolve(config, _ => null));

        Assert.Contains("Foundry:ProjectEndpoint", ex.Message);
        Assert.Contains("FOUNDRY_PROJECT_ENDPOINT", ex.Message);
    }

    [Fact]
    public void MissingModelThrowsActionableError()
    {
        var config = new Dictionary<string, string?>
        {
            ["Foundry:ProjectEndpoint"] = "https://project.services.ai.azure.com/api/projects/demo"
        };

        var ex = Assert.Throws<ConfigurationValidationException>(
            () => FoundryConfiguration.Resolve(config, _ => null));

        Assert.Contains("Foundry:Model", ex.Message);
        Assert.Contains("FOUNDRY_MODEL", ex.Message);
    }
}

public sealed class OfflineClawTests
{
    [Fact]
    public async Task OfflineStockScenarioIsDeterministic()
    {
        var writer = new StringWriter(new StringBuilder());
        var claw = new OfflineClaw(CreateStockTools());

        await claw.RunScenarioAsync("stock", writer, CancellationToken.None);

        var output = Normalize(writer.ToString());
        Assert.Equal(
            "SCENARIO stock\nMSFT: 512.34 USD (mock)\nNVDA: 184.72 USD (mock)\n",
            output);
    }

    [Fact]
    public async Task OfflinePlanScenarioDoesNotExecuteWithoutApproval()
    {
        var writer = new StringWriter(new StringBuilder());
        var claw = new OfflineClaw(CreateStockTools());

        await claw.RunScenarioAsync("plan", writer, CancellationToken.None);

        var output = Normalize(writer.ToString());
        Assert.Contains("SCENARIO plan", output);
        Assert.Contains("Approval granted: no", output);
        Assert.Contains("Execution triggered: no", output);
        Assert.Contains("Execution blocked until explicit approval.", output);
    }

    [Fact]
    public void PlanApprovalGateBlocksExecutionWithoutApproval()
    {
        var planning = new PlanningResponse
        {
            Type = PlanningResponseType.Approval,
            Questions = [new PlanningQuestion { Message = "Execute plan" }]
        };

        var executed = false;
        var result = PlanApprovalGate.TryExecute(planning, isApproved: false, () => executed = true);

        Assert.False(result);
        Assert.False(executed);
    }

    [Fact]
    public void PlanApprovalGateAllowsApprovedGeneratedPlanExecution()
    {
        var planning = new PlanningResponse
        {
            Type = PlanningResponseType.Approval,
            Questions = [new PlanningQuestion { Message = "Execute generated plan" }]
        };

        var executed = false;
        var result = PlanApprovalGate.TryExecute(
            planning,
            isApproved: true,
            () => executed = true);

        Assert.True(result);
        Assert.True(executed);
    }

    private static StockTools CreateStockTools()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "mock-market-data.json");
        return new StockTools(path);
    }

    private static string Normalize(string value) => value.Replace("\r\n", "\n");
}

public sealed class ProgramEntryFixtureSafetyTests
{
    [Fact]
    public void MissingFixtureFailureIsSanitized()
    {
        var writer = new StringWriter(new StringBuilder());

        var created = ProgramEntry.TryCreateStockTools(
            () => throw new FileNotFoundException(
                "Missing fixture at C:\\private\\project\\mock-market-data.json.",
                "C:\\private\\project\\mock-market-data.json"),
            static path => new StockTools(path),
            writer,
            out var stockTools);

        Assert.False(created);
        Assert.Null(stockTools);
        Assert.Equal(
            ProgramEntry.FixtureFailureMessage + Environment.NewLine,
            writer.ToString());
        Assert.DoesNotContain("C:\\private", writer.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FileNotFoundException", writer.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MalformedFixtureFailureIsSanitized()
    {
        var writer = new StringWriter(new StringBuilder());

        var created = ProgramEntry.TryCreateStockTools(
            static () => "mock-market-data.json",
            static _ => throw new System.Text.Json.JsonException(
                "Malformed JSON near private fixture details and byte position 42."),
            writer,
            out var stockTools);

        Assert.False(created);
        Assert.Null(stockTools);
        Assert.Equal(
            ProgramEntry.FixtureFailureMessage + Environment.NewLine,
            writer.ToString());
        Assert.DoesNotContain("byte position", writer.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("JsonException", writer.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidFixtureCreatesStockToolsWithoutError()
    {
        var writer = new StringWriter(new StringBuilder());
        var path = Path.Combine(AppContext.BaseDirectory, "mock-market-data.json");

        var created = ProgramEntry.TryCreateStockTools(
            () => path,
            static fixturePath => new StockTools(fixturePath),
            writer,
            out var stockTools);

        Assert.True(created);
        Assert.NotNull(stockTools);
        Assert.Equal(string.Empty, writer.ToString());
    }
}

public sealed class LiveOutputContractTests
{
    [Fact]
    public void CanonicalLiveOutputUsesExplicitAccurateMarkers()
    {
        Assert.Equal("LIVE · mafclaw · Session 01", ClawConsole.LiveBanner);
        Assert.Equal(
            "Mode starts in plan. Commands: /mode [plan|execute], /todos, /exit",
            ClawConsole.CommandBanner);
        Assert.Equal("[Hosted web search was used.]", ClawConsole.HostedSearchUsedMarker);
    }
}

/// <summary>
/// Proves user-secret configuration loads through the injectable IConfiguration path
/// without accessing the real secret store.
/// </summary>
public sealed class FoundryConfigurationUserSecretsPathTests
{
    [Fact]
    public void UserSecretValuesResolvedViaInMemoryConfigurationProvider()
    {
        // Simulate the values that `dotnet user-secrets set` would place in the store.
        // ConfigurationBuilder + AddInMemoryCollection mirrors the IConfiguration
        // shape that AddUserSecrets<T>() produces at runtime — no real secret store accessed.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Foundry:ProjectEndpoint"] = "https://usersecrets.services.ai.azure.com/api/projects/myproject",
                ["Foundry:Model"] = "gpt-5.4"
            })
            .Build();

        var settings = FoundryConfiguration.Resolve(configuration, _ => null);

        Assert.Equal("https://usersecrets.services.ai.azure.com/api/projects/myproject", settings.ProjectEndpoint);
        Assert.Equal("gpt-5.4", settings.Model);
    }

    [Fact]
    public void CanonicalConfigValuesTakePrecedenceOverAliasEnvVars()
    {
        // A value present in IConfiguration (appsettings.json or user-secrets) must win
        // over the FOUNDRY_* alias environment variables.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Foundry:ProjectEndpoint"] = "https://config.services.ai.azure.com/api/projects/canonical",
                ["Foundry:Model"] = "canonical-model"
            })
            .Build();

        var aliasEnv = new Dictionary<string, string?>
        {
            ["FOUNDRY_PROJECT_ENDPOINT"] = "https://alias.services.ai.azure.com/api/projects/alias",
            ["FOUNDRY_MODEL"] = "alias-model"
        };

        var settings = FoundryConfiguration.Resolve(
            configuration,
            key => aliasEnv.TryGetValue(key, out var val) ? val : null);

        Assert.Equal("https://config.services.ai.azure.com/api/projects/canonical", settings.ProjectEndpoint);
        Assert.Equal("canonical-model", settings.Model);
    }

    [Fact]
    public void AliasEnvVarIsUsedWhenConfigurationAndCanonicalEnvFormAreAbsent()
    {
        // When neither canonical config nor canonical env form (Foundry__*) is present,
        // the documented FOUNDRY_* alias env vars must be the final fallback.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var aliasEnv = new Dictionary<string, string?>
        {
            ["FOUNDRY_PROJECT_ENDPOINT"] = "https://alias.services.ai.azure.com/api/projects/fallback",
            ["FOUNDRY_MODEL"] = "alias-fallback-model"
        };

        var settings = FoundryConfiguration.Resolve(
            configuration,
            key => aliasEnv.TryGetValue(key, out var val) ? val : null);

        Assert.Equal("https://alias.services.ai.azure.com/api/projects/fallback", settings.ProjectEndpoint);
        Assert.Equal("alias-fallback-model", settings.Model);
    }

    [Fact]
    public void MissingAllSourcesThrowsWithUserSecretsGuidanceInMessage()
    {
        // With empty config and no env vars, the error must name the missing key and
        // include actionable `dotnet user-secrets set` guidance — but must never print
        // a real secret value.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var ex = Assert.Throws<ConfigurationValidationException>(
            () => FoundryConfiguration.Resolve(configuration, _ => null));

        Assert.Contains("Foundry:ProjectEndpoint", ex.Message);
        Assert.Contains("dotnet user-secrets set", ex.Message);
        Assert.Contains("FOUNDRY_PROJECT_ENDPOINT", ex.Message);
    }
}

/// <summary>
/// Regression suite for <see cref="AuthErrorFormatter"/>.
/// Proves that every formatted message contains actionable generic guidance
/// and does not include sensitive terms that leak from raw exception messages.
/// </summary>
public sealed class AuthErrorFormatterTests
{
    // ── Credential unavailable ───────────────────────────────────────────────

    [Fact]
    public void CredentialUnavailable_ContainsAzLoginGuidance()
    {
        var msg = AuthErrorFormatter.CredentialUnavailable();
        Assert.Contains("az login", msg);
    }

    [Fact]
    public void CredentialUnavailable_MentionsTenant()
    {
        var msg = AuthErrorFormatter.CredentialUnavailable();
        Assert.Contains("tenant", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("DefaultAzureCredential")]
    [InlineData("EnvironmentCredential")]
    [InlineData("SharedTokenCacheCredential")]
    [InlineData("ManagedIdentityCredential")]
    [InlineData("tenant_id")]
    [InlineData("correlation_id")]
    [InlineData("claims")]
    [InlineData("access_token")]
    [InlineData("Exception")]
    public void CredentialUnavailable_DoesNotLeakSensitiveTerm(string sensitiveToken)
    {
        var msg = AuthErrorFormatter.CredentialUnavailable();
        Assert.DoesNotContain(sensitiveToken, msg, StringComparison.OrdinalIgnoreCase);
    }

    // ── Authentication failed ────────────────────────────────────────────────

    [Fact]
    public void AuthenticationFailed_ContainsAzLoginGuidance()
    {
        var msg = AuthErrorFormatter.AuthenticationFailed();
        Assert.Contains("az login", msg);
    }

    [Fact]
    public void AuthenticationFailed_MentionsTenant()
    {
        var msg = AuthErrorFormatter.AuthenticationFailed();
        Assert.Contains("tenant", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("DefaultAzureCredential")]
    [InlineData("EnvironmentCredential")]
    [InlineData("SharedTokenCacheCredential")]
    [InlineData("tenant_id")]
    [InlineData("correlation_id")]
    [InlineData("claims")]
    [InlineData("access_token")]
    [InlineData("Exception")]
    public void AuthenticationFailed_DoesNotLeakSensitiveTerm(string sensitiveToken)
    {
        var msg = AuthErrorFormatter.AuthenticationFailed();
        Assert.DoesNotContain(sensitiveToken, msg, StringComparison.OrdinalIgnoreCase);
    }

    // ── Request failed ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(429)]
    [InlineData(503)]
    public void RequestFailed_ContainsStatusCode(int code)
    {
        var msg = AuthErrorFormatter.RequestFailed(code);
        Assert.Contains(code.ToString(), msg);
    }

    [Fact]
    public void RequestFailed_ContainsActionableGuidance()
    {
        var msg = AuthErrorFormatter.RequestFailed(503);
        Assert.Contains("configuration", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("https://")]
    [InlineData("endpoint")]
    [InlineData("Exception")]
    [InlineData("correlation_id")]
    [InlineData("request_id")]
    public void RequestFailed_DoesNotLeakSensitiveTerm(string sensitiveToken)
    {
        var msg = AuthErrorFormatter.RequestFailed(503);
        Assert.DoesNotContain(sensitiveToken, msg, StringComparison.OrdinalIgnoreCase);
    }

    // ── Network failure ──────────────────────────────────────────────────────

    [Fact]
    public void NetworkFailure_MentionsNetworkOrConnectivity()
    {
        var msg = AuthErrorFormatter.NetworkFailure();
        Assert.True(
            msg.Contains("network", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("connectivity", StringComparison.OrdinalIgnoreCase),
            $"Expected network/connectivity guidance, got: {msg}");
    }

    [Theory]
    [InlineData("https://")]
    [InlineData("Exception")]
    [InlineData("SocketError")]
    [InlineData("request_id")]
    public void NetworkFailure_DoesNotLeakSensitiveTerm(string sensitiveToken)
    {
        var msg = AuthErrorFormatter.NetworkFailure();
        Assert.DoesNotContain(sensitiveToken, msg, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class FinanceAgentFactoryOptionsTests
{
    [Fact]
    public void BuiltOptionsHaveDisableFileMemorySet()
    {
        var chatOptions = new ChatOptions { Instructions = "test" };
        var options = FinanceAgentFactory.BuildOptions(chatOptions);

        Assert.True(options.DisableFileMemory,
            "Session 1 must never activate Harness FileMemoryProvider. Set DisableFileMemory=true.");
    }

    [Fact]
    public void BuiltOptionsPreserveChatOptions()
    {
        var chatOptions = new ChatOptions { Instructions = "my instructions" };
        var options = FinanceAgentFactory.BuildOptions(chatOptions);

        Assert.Same(chatOptions, options.ChatOptions);
    }

    [Fact]
    public void BuiltOptionsEnableHostedWebSearch()
    {
        var options = FinanceAgentFactory.BuildOptions(new ChatOptions());

        Assert.False(options.DisableWebSearch,
            "Hosted web search is part of the Session 1 live Harness experience.");
    }

    [Fact]
    public void BuiltOptionsDisableHarnessModeProvider()
    {
        var options = FinanceAgentFactory.BuildOptions(new ChatOptions());

        Assert.True(options.DisableAgentModeProvider,
            "Console-managed mode changes avoid the Harness transition prompt that some Foundry safety policies reject.");
    }

    [Fact]
    public void BuiltOptionsHaveExpectedAgentName()
    {
        var options = FinanceAgentFactory.BuildOptions(new ChatOptions());

        Assert.Equal("mafclaw-session-01", options.Name);
    }
}

public sealed class OfflineClawSymbolHandlingTests
{
    [Fact]
    public async Task UnsupportedSymbolPrintsErrorAndContinues()
    {
        // TSLA is not in mock-market-data.json (KeyNotFoundException path)
        var (claw, writer) = CreateClawAndWriter();
        var reader = new StringReader("/stock TSLA\n/exit\n");

        await claw.RunAsync(null, reader, writer, CancellationToken.None);

        var output = Normalize(writer.ToString());
        Assert.Contains("TSLA", output);
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Exiting offline mode.", output);
    }

    [Fact]
    public async Task MalformedSymbolPrintsErrorAndContinues()
    {
        // MSFT! fails the SymbolPattern regex (ArgumentException path)
        var (claw, writer) = CreateClawAndWriter();
        var reader = new StringReader("/stock MSFT!\n/exit\n");

        await claw.RunAsync(null, reader, writer, CancellationToken.None);

        var output = Normalize(writer.ToString());
        Assert.Contains("Invalid symbol", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Exiting offline mode.", output);
    }

    [Fact]
    public async Task LookUpStockAsyncUnsupportedSymbolWritesMessage()
    {
        var (claw, writer) = CreateClawAndWriter();

        await claw.LookUpStockAsync("TSLA", writer);

        var output = writer.ToString();
        Assert.Contains("TSLA", output);
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LookUpStockAsyncMalformedSymbolWritesMessage()
    {
        var (claw, writer) = CreateClawAndWriter();

        await claw.LookUpStockAsync("MSFT!", writer);

        var output = writer.ToString();
        Assert.Contains("Invalid symbol", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LookUpStockAsyncBlankSymbolWritesMessage()
    {
        var (claw, writer) = CreateClawAndWriter();

        await claw.LookUpStockAsync("   ", writer);

        var output = writer.ToString();
        Assert.Contains("Invalid symbol", output, StringComparison.OrdinalIgnoreCase);
    }

    private static (OfflineClaw claw, StringWriter writer) CreateClawAndWriter()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "mock-market-data.json");
        var tools = new StockTools(path);
        return (new OfflineClaw(tools), new StringWriter(new StringBuilder()));
    }

    private static string Normalize(string value) => value.Replace("\r\n", "\n");
}

/// <summary>
/// Regression suite for <see cref="ErrorDispatch"/>.
/// Confirms each known exception kind routes to the correct exit code and that
/// injected raw exception messages (URLs, tenant/account IDs, claims, tokens,
/// trace/correlation IDs, response bodies) never surface in the output.
/// </summary>
public sealed class ErrorDispatchTests
{
    // ── ClientResultException (System.ClientModel / OpenAI SDK) ─────────────

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(429)]
    [InlineData(503)]
    public void ClientResultException_MapsToExitCode4WithStatusInMessage(int status)
    {
        var raw = $"HTTP {status}: https://eastus.api.cognitive.microsoft.com/projects/myproj " +
                  "tenant_id=11111111-1111-1111-1111-111111111111 " +
                  "access_token=eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.REDACTED.sig " + // mafclaw-secret-scan: allow-test-fixture
                  "claims=[oid:22222222-2222-2222-2222-222222222222] " +
                  "x-ms-client-request-id=trace-abc-123 " +
                  "response:{\"error\":{\"code\":\"AuthorizationFailed\",\"message\":\"Access denied\"}}";
        var ex = new ClientResultException(raw, new MockPipelineResponse(status), null);

        var result = ErrorDispatch.TryMap(ex);

        Assert.NotNull(result);
        Assert.Equal(4, result!.Value.exitCode);
        Assert.Contains(status.ToString(), result.Value.message);
    }

    [Theory]
    [InlineData("https://")]
    [InlineData("eastus.api.cognitive.microsoft.com")]
    [InlineData("tenant_id")]
    [InlineData("11111111-1111-1111-1111-111111111111")]
    [InlineData("access_token")]
    [InlineData("eyJhbGci")]
    [InlineData("claims")]
    [InlineData("oid:22222222")]
    [InlineData("x-ms-client-request-id")]
    [InlineData("trace-abc-123")]
    [InlineData("AuthorizationFailed")]
    [InlineData("Access denied")]
    [InlineData("Exception")]
    public void ClientResultException_DoesNotLeakSensitiveTermFromRawMessage(string sensitiveToken)
    {
        var raw = "Endpoint: https://secret.tenant.azure.com/token?client_id=44444444 " +
                  "tenant_id=11111111-1111-1111-1111-111111111111 " +
                  "access_token=eyJhbGciOiJSUzI1NiJ9.PAYLOAD.sig " + // mafclaw-secret-scan: allow-test-fixture
                  "claims=[upn:user@corp.example.com] " +
                  "x-ms-client-request-id=trace-abc-123 " +
                  "response:{\"error\":{\"code\":\"AuthorizationFailed\",\"message\":\"Access denied\"}}";
        var ex = new ClientResultException(raw, new MockPipelineResponse(403), null);

        var result = ErrorDispatch.TryMap(ex)!.Value;

        Assert.DoesNotContain(sensitiveToken, result.message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClientResultException_ZeroStatusStillMapsToExitCode4()
    {
        var ex = new ClientResultException("service error, no status", new MockPipelineResponse(0), null);

        var result = ErrorDispatch.TryMap(ex);

        Assert.NotNull(result);
        Assert.Equal(4, result!.Value.exitCode);
    }

    // ── CredentialUnavailableException ───────────────────────────────────────

    [Fact]
    public void CredentialUnavailableException_MapsToExitCode3()
    {
        var raw = "DefaultAzureCredential failed to retrieve a token from the included credentials. " +
                  "EnvironmentCredential: tenant_id=33333333-3333-3333-3333-333333333333 " +
                  "SharedTokenCacheCredential: correlation_id=xyz789 " +
                  "ManagedIdentityCredential: access_token=eyJhbGci claims=[aud:api://default]";
        var ex = new CredentialUnavailableException(raw);

        var result = ErrorDispatch.TryMap(ex);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Value.exitCode);
    }

    [Theory]
    [InlineData("DefaultAzureCredential")]
    [InlineData("EnvironmentCredential")]
    [InlineData("SharedTokenCacheCredential")]
    [InlineData("ManagedIdentityCredential")]
    [InlineData("tenant_id")]
    [InlineData("33333333-3333-3333-3333-333333333333")]
    [InlineData("correlation_id")]
    [InlineData("xyz789")]
    [InlineData("access_token")]
    [InlineData("eyJhbGci")]
    [InlineData("claims")]
    [InlineData("Exception")]
    public void CredentialUnavailableException_DoesNotLeakSensitiveTerm(string sensitiveToken)
    {
        var raw = "DefaultAzureCredential failed. EnvironmentCredential: tenant_id=33333333, " +
                  "SharedTokenCacheCredential: correlation_id=xyz789, " +
                  "ManagedIdentityCredential: access_token=eyJhbGci claims=[aud:api://default]";
        var ex = new CredentialUnavailableException(raw);

        var result = ErrorDispatch.TryMap(ex)!.Value;

        Assert.DoesNotContain(sensitiveToken, result.message, StringComparison.OrdinalIgnoreCase);
    }

    // ── AuthenticationFailedException ────────────────────────────────────────

    [Fact]
    public void AuthenticationFailedException_MapsToExitCode3()
    {
        var raw = "AADSTS70011: The provided client_id=44444444-4444-4444-4444-444444444444 has invalid claims. " +
                  "https://login.microsoftonline.com/55555555-5555-5555-5555-555555555555/token " +
                  "response:{\"error\":\"invalid_client\",\"error_description\":\"AADSTS70011\"}";
        var ex = new AuthenticationFailedException(raw);

        var result = ErrorDispatch.TryMap(ex);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Value.exitCode);
    }

    [Theory]
    [InlineData("AADSTS70011")]
    [InlineData("client_id")]
    [InlineData("44444444-4444-4444-4444-444444444444")]
    [InlineData("https://")]
    [InlineData("login.microsoftonline.com")]
    [InlineData("55555555-5555-5555-5555-555555555555")]
    [InlineData("invalid_client")]
    [InlineData("error_description")]
    [InlineData("Exception")]
    public void AuthenticationFailedException_DoesNotLeakSensitiveTerm(string sensitiveToken)
    {
        var raw = "AADSTS70011: client_id=44444444-4444-4444-4444-444444444444 " +
                  "https://login.microsoftonline.com/55555555/token " +
                  "response:{\"error\":\"invalid_client\",\"error_description\":\"AADSTS70011\"}";
        var ex = new AuthenticationFailedException(raw);

        var result = ErrorDispatch.TryMap(ex)!.Value;

        Assert.DoesNotContain(sensitiveToken, result.message, StringComparison.OrdinalIgnoreCase);
    }

    // ── RequestFailedException (Azure SDK) ───────────────────────────────────

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(429)]
    [InlineData(503)]
    public void RequestFailedException_MapsToExitCode4WithStatusInMessage(int status)
    {
        var raw = $"Operation returned status {status}. " +
                  "Endpoint: https://project.api.azureml.ms/subscriptions/abc123 " +
                  "request_id=11111111 correlation_id=22222222 " +
                  "response:{\"error\":{\"code\":\"AuthorizationFailed\"}}";
        var ex = new RequestFailedException(status, raw);

        var result = ErrorDispatch.TryMap(ex);

        Assert.NotNull(result);
        Assert.Equal(4, result!.Value.exitCode);
        Assert.Contains(status.ToString(), result.Value.message);
    }

    [Theory]
    [InlineData("https://")]
    [InlineData("project.api.azureml.ms")]
    [InlineData("request_id")]
    [InlineData("11111111")]
    [InlineData("correlation_id")]
    [InlineData("22222222")]
    [InlineData("AuthorizationFailed")]
    [InlineData("Exception")]
    public void RequestFailedException_DoesNotLeakSensitiveTerm(string sensitiveToken)
    {
        var raw = "Operation returned status 403. " +
                  "Endpoint: https://project.api.azureml.ms/subscriptions/abc123 " +
                  "request_id=11111111 correlation_id=22222222 " +
                  "response:{\"error\":{\"code\":\"AuthorizationFailed\"}}";
        var ex = new RequestFailedException(403, raw);

        var result = ErrorDispatch.TryMap(ex)!.Value;

        Assert.DoesNotContain(sensitiveToken, result.message, StringComparison.OrdinalIgnoreCase);
    }

    // ── HttpRequestException ─────────────────────────────────────────────────

    [Fact]
    public void HttpRequestException_MapsToExitCode5()
    {
        var raw = "Connection refused. Host: eastus.api.cognitive.microsoft.com:443 " +
                  "SocketError: ConnectionRefused SystemException trace at System.Net.Http.HttpClientHandler";
        var ex = new HttpRequestException(raw);

        var result = ErrorDispatch.TryMap(ex);

        Assert.NotNull(result);
        Assert.Equal(5, result!.Value.exitCode);
    }

    [Theory]
    [InlineData("eastus.api.cognitive.microsoft.com")]
    [InlineData("SocketError")]
    [InlineData("ConnectionRefused")]
    [InlineData("SystemException")]
    [InlineData("HttpClientHandler")]
    [InlineData("Exception")]
    public void HttpRequestException_DoesNotLeakSensitiveTerm(string sensitiveToken)
    {
        var raw = "Connection refused. Host: eastus.api.cognitive.microsoft.com " +
                  "SocketError: ConnectionRefused trace at System.Net.Http.HttpClientHandler";
        var ex = new HttpRequestException(raw);

        var result = ErrorDispatch.TryMap(ex)!.Value;

        Assert.DoesNotContain(sensitiveToken, result.message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Unknown exception must not be mapped ─────────────────────────────────

    [Fact]
    public void UnknownException_ReturnsNull()
    {
        var ex = new InvalidOperationException("programming error — should propagate");

        var result = ErrorDispatch.TryMap(ex);

        Assert.Null(result);
    }

    [Fact]
    public void NullReferenceException_ReturnsNull()
    {
        var ex = new NullReferenceException("null deref — should propagate");

        var result = ErrorDispatch.TryMap(ex);

        Assert.Null(result);
    }
}

/// <summary>
/// Proves <see cref="FinanceAgentFactory"/> selects <see cref="AzureCliCredential"/> by default
/// and that the injectable credential factory is honoured — without invoking real authentication.
/// </summary>
public sealed class FinanceAgentFactoryCredentialTests
{
    [Fact]
    public void DefaultCredentialFactoryProducesAzureCliCredential()
    {
        var credential = FinanceAgentFactory.DefaultCredentialFactory();
        Assert.IsType<AzureCliCredential>(credential);
    }

    [Fact]
    public void DefaultFactoryIsUsedWhenNoOverrideProvided()
    {
        var factory = new FinanceAgentFactory(MakeSettings(), MakeTools());
        var credential = factory.BuildCredential();
        Assert.IsType<AzureCliCredential>(credential);
    }

    [Fact]
    public void InjectableCredentialFactoryIsSelectedOverDefault()
    {
        var stub = new StubTokenCredential();
        var factory = new FinanceAgentFactory(MakeSettings(), MakeTools(), credentialFactory: () => stub);

        var credential = factory.BuildCredential();

        Assert.Same(stub, credential);
        Assert.IsType<StubTokenCredential>(credential);
    }

    [Fact]
    public void InjectableCredentialFactoryIsInvokedEachCall()
    {
        var callCount = 0;
        var factory = new FinanceAgentFactory(MakeSettings(), MakeTools(), credentialFactory: () =>
        {
            callCount++;
            return new StubTokenCredential();
        });

        factory.BuildCredential();
        factory.BuildCredential();

        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task CreateAsync_PreflightsCredentialWithAiAzureAudienceScope()
    {
        var recording = new RecordingTokenCredential();
        var factory = new FinanceAgentFactory(MakeSettings(), MakeTools(), credentialFactory: () => recording);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.CreateAsync(CancellationToken.None));

        Assert.Equal("Preflight probe stop.", ex.Message);
        Assert.Equal(1, recording.AsyncCalls);
        Assert.Equal(["https://ai.azure.com/.default"], recording.RecordedScopes);
    }

    private static FoundrySettings MakeSettings() =>
        new("https://dummy.services.ai.azure.com/api/projects/test", "gpt-dummy");

    private static StockTools MakeTools() =>
        new(Path.Combine(AppContext.BaseDirectory, "mock-market-data.json"));
}

/// <summary>
/// Minimal <see cref="TokenCredential"/> stub for unit tests that must not invoke real authentication.
/// </summary>
internal sealed class StubTokenCredential : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        => throw new NotSupportedException("Stub credential — not for real auth.");

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        => throw new NotSupportedException("Stub credential — not for real auth.");
}

internal sealed class RecordingTokenCredential : TokenCredential
{
    private readonly List<string> _recordedScopes = [];

    public int AsyncCalls { get; private set; }
    public IReadOnlyList<string> RecordedScopes => _recordedScopes;

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        => throw new NotSupportedException("Recording credential is async-only for preflight tests.");

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        AsyncCalls++;
        _recordedScopes.Clear();
        _recordedScopes.AddRange(requestContext.Scopes);
        throw new InvalidOperationException("Preflight probe stop.");
    }
}

/// <summary>
/// Minimal concrete <see cref="PipelineResponse"/> for constructing
/// <see cref="ClientResultException"/> instances in tests.
/// </summary>
internal sealed class MockPipelineResponse : PipelineResponse
{
    private readonly int _status;
    private Stream _contentStream = Stream.Null;
    private readonly MockPipelineResponseHeaders _headers = new();

    public MockPipelineResponse(int status) => _status = status;

    public override int Status => _status;
    public override string ReasonPhrase => string.Empty;
    protected override PipelineResponseHeaders HeadersCore => _headers;
    public override Stream? ContentStream
    {
        get => _contentStream;
        set => _contentStream = value ?? Stream.Null;
    }
    public override BinaryData Content => BinaryData.Empty;

    public override BinaryData BufferContent(CancellationToken cancellationToken = default)
        => BinaryData.Empty;

    public override ValueTask<BinaryData> BufferContentAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(BinaryData.Empty);

    public override void Dispose() => _contentStream.Dispose();
}

internal sealed class MockPipelineResponseHeaders : PipelineResponseHeaders
{
    public override bool TryGetValue(string name, out string? value)
    {
        value = null;
        return false;
    }

    public override bool TryGetValues(string name, out IEnumerable<string>? values)
    {
        values = null;
        return false;
    }

    public override IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        => Enumerable.Empty<KeyValuePair<string, string>>().GetEnumerator();
}
