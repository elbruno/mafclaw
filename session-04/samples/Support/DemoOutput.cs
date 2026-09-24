// Objective: print teaching evidence consistently without hiding agent construction in a helper.
// A. Show actual tool receipts before the assistant's text.
// B. Surface failures without dumping provider payloads or configuration values.

using System.ClientModel;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.Samples;

public static class DemoOutput
{
    public static void Print(AgentResponse response)
    {
        // A. These samples contain only synthetic inputs or public documentation.
        foreach (var result in response.Messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>())
            Console.WriteLine($"TOOL RESULT [{result.CallId}]: {JsonSerializer.Serialize(result.Result)}");
        if (!string.IsNullOrWhiteSpace(response.Text)) Console.WriteLine(response.Text);
    }

    public static int Report(Exception exception)
    {
        // B. Exception type is useful on stream; provider messages may include private endpoints.
        Console.Error.WriteLine($"SAMPLE FAILED: {exception.GetType().Name}. Check the sample's setup and mode.");
        if (exception is ClientResultException modelError)
            Console.Error.WriteLine($"Model request failed (HTTP {modelError.Status}).");
        return 1;
    }
}
