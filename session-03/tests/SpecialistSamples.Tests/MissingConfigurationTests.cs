// Objective: verify safe live configuration failures without reading real user secrets.
// A. Redirect the child process's user-secrets roots to a nonexistent owned path.
// B. Override endpoint environment aliases with empty values before startup.
// C. Assert an explicit nonzero missing-config error and no fallback.

using System.Diagnostics;

namespace MafClaw.SpecialistSamples.Tests;

public static class MissingConfigurationTests
{
    public static async Task RunAsync()
    {
        foreach (string assembly in new[]
        {
            typeof(MafClaw.Sample42.SampleApp).Assembly.Location,
            typeof(MafClaw.Sample43.SampleApp).Assembly.Location
        })
        {
            string emptyRoot = Path.Combine(AppContext.BaseDirectory, "missing-configuration-fixture");
            Check.True(!Directory.Exists(emptyRoot), "The isolated configuration root must not exist.");
            var start = new ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            start.ArgumentList.Add(assembly);
            start.ArgumentList.Add("--mode");
            start.ArgumentList.Add("live");
            start.ArgumentList.Add("--prompt");
            start.ArgumentList.Add("This must fail configuration before model access.");
            start.Environment["APPDATA"] = emptyRoot;
            start.Environment["HOME"] = emptyRoot;
            start.Environment["DOTNET_USER_SECRETS_FALLBACK_DIR"] = emptyRoot;
            start.Environment["Foundry__ProjectEndpoint"] = "";
            start.Environment["Foundry:ProjectEndpoint"] = "";
            start.Environment["FOUNDRY_PROJECT_ENDPOINT"] = "";
            start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
            using Process child = Process.Start(start) ?? throw new InvalidOperationException("Could not start configuration test.");
            Task<string> stdout = child.StandardOutput.ReadToEndAsync();
            Task<string> stderr = child.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await child.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                child.Kill(entireProcessTree: true);
                throw new InvalidOperationException("Missing configuration did not fail within the bounded test.");
            }
            string error = await stderr;
            Check.True(child.ExitCode != 0, "Missing live configuration must exit nonzero.");
            Check.True(error.Contains("Missing Foundry:ProjectEndpoint", StringComparison.Ordinal), "Safe missing-key diagnostic.");
            Check.True(!error.Contains("https://", StringComparison.OrdinalIgnoreCase), "No endpoint disclosure.");
            Check.True(!(await stdout).Contains("SCRIPTED FIXTURE INFERENCE", StringComparison.Ordinal), "No silent fallback.");
        }
    }
}
