using Azure;
using Azure.Identity;
using MafClaw.Session01;
using System.ClientModel;

return await ProgramEntry.RunAsync(args);

internal static class ProgramEntry
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (!CliOptions.TryParse(args, out var options, out var parseError))
        {
            if (!string.IsNullOrWhiteSpace(parseError))
            {
                Console.Error.WriteLine(parseError);
            }

            CliOptions.PrintUsage(Console.Error);
            return 1;
        }

        var stockTools = new StockTools(ResolveMarketDataPath());

        if (options.Mode == RuntimeMode.Offline)
        {
            var offlineClaw = new OfflineClaw(stockTools);
            await offlineClaw.RunAsync(options.Scenario, Console.In, Console.Out, CancellationToken.None);
            return 0;
        }

        try
        {
            var settings = FoundryConfiguration.Resolve();
            var runtime = await new FinanceAgentFactory(settings, stockTools).CreateAsync(CancellationToken.None);

            var console = new ClawConsole(runtime.Agent, runtime.TodoProvider);
            await console.RunAsync(Console.In, Console.Out, CancellationToken.None);
            return 0;
        }
        catch (ConfigurationValidationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
        catch (CredentialUnavailableException ex)
        {
            return WriteError(ErrorDispatch.TryMap(ex)!.Value);
        }
        catch (AuthenticationFailedException ex)
        {
            return WriteError(ErrorDispatch.TryMap(ex)!.Value);
        }
        catch (ClientResultException ex)
        {
            return WriteError(ErrorDispatch.TryMap(ex)!.Value);
        }
        catch (RequestFailedException ex)
        {
            return WriteError(ErrorDispatch.TryMap(ex)!.Value);
        }
        catch (HttpRequestException ex)
        {
            return WriteError(ErrorDispatch.TryMap(ex)!.Value);
        }
    }

    private static int WriteError((string message, int exitCode) mapped)
    {
        Console.Error.WriteLine(mapped.message);
        return mapped.exitCode;
    }

    private static string ResolveMarketDataPath()
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, "mock-market-data.json");
        if (File.Exists(outputPath))
        {
            return outputPath;
        }

        var localPath = Path.Combine(Directory.GetCurrentDirectory(), "mock-market-data.json");
        if (File.Exists(localPath))
        {
            return localPath;
        }

        throw new FileNotFoundException("mock-market-data.json was not found.", localPath);
    }
}
