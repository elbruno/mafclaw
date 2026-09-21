// Objective: surface failures without disclosing provider payloads or infrastructure.
// A. Show application-authored configuration errors.
// B. Categorize provider and cancellation failures without printing raw messages.
namespace MafClaw.Session04;

public static class SafeErrors
{
    public static int Report(Exception exception)
    {
        var message = exception switch
        {
            FinanceConfigurationException configuration => configuration.Message,
            FinanceUpstreamException upstream => upstream.Message,
            OperationCanceledException => "Operation cancelled or deadline exceeded.",
            Azure.Identity.AuthenticationFailedException => "Authentication failed. Sign in privately.",
            Azure.RequestFailedException request => $"Azure request failed (HTTP {request.Status}).",
            System.ClientModel.ClientResultException result => $"Model request failed (HTTP {result.Status}).",
            _ => $"Operation failed ({exception.GetType().Name}). Inspect diagnostics in a private debugger."
        };
        Console.Error.WriteLine(message);
        return 1;
    }
}
