// Objective: keep expected setup and service failures explicit and safe to show.
// Steps:
// A. Run the sample's own entry point.
// B. Distinguish cancellation and configuration problems.
// C. Report expected transport/fixture errors without raw cloud identifiers.

using System.ClientModel;
using System.Text.Json;
using Azure;
using Azure.Identity;

namespace MafClaw.OrchestrationSupport;

public static class SafeConsole
{
    public static async Task<int> RunAsync(Func<Task<int>> run)
    {
        try
        {
            return await run();
        }
        catch (SampleConfigurationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Run cancelled or its time budget expired; no successful completion is implied.");
            return 130;
        }
        catch (AuthenticationFailedException)
        {
            Console.Error.WriteLine("Azure authentication failed. Sign in privately before running --mode live.");
            return 1;
        }
        catch (RequestFailedException exception)
        {
            Console.Error.WriteLine($"Foundry request failed (HTTP {exception.Status}). Check service settings privately.");
            return 1;
        }
        catch (ClientResultException exception)
        {
            Console.Error.WriteLine($"Model request failed (HTTP {exception.Status}). Check model availability privately.");
            return 1;
        }
        catch (HttpRequestException)
        {
            Console.Error.WriteLine("The model connection failed. Check connectivity privately.");
            return 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            JsonException or FormatException or ArgumentException or InvalidOperationException)
        {
            Console.Error.WriteLine($"Sample stopped ({exception.GetType().Name}). Check arguments, local fixtures, and the preceding host events.");
            return 1;
        }
    }
}
