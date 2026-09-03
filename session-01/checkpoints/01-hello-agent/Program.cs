using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>().AddEnvironmentVariables().Build();
var endpoint = config["Foundry:ProjectEndpoint"]!;
var model = config["Foundry:Model"] ?? "gpt-5-mini";

AIAgent agent = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
    .AsAIAgent(
        model: model,
        instructions: "You are a personal finance education assistant. Keep responses concise.",
        name: "mafclaw-hello");

Console.WriteLine(await agent.RunAsync(
    "What are three things a beginner should consider before buying a stock?"));
