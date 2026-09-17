// Objective: give the model narrow, named MAF roles.
// A. Build tool-free writer and reviewer agents with AsAIAgent.
// B. Give the main Harness only host-enforced workflow functions.
// C. Disable unrelated capabilities and bound the function-invocation loop.

using MafClaw.OrchestrationSupport;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample44;

public static class WorkflowAgents
{
    public static AIAgent Writer(IChatClient client, Transcript transcript) =>
        Specialist(client, transcript, "WriterAgent",
            "Write only a fictional educational report from the supplied observations. " +
            "Cite all provided [MOCK-...] identifiers. Do not invent facts or predict returns. " +
            $"Always include: {DraftCheck.RequiredDisclaimer}");

    public static AIAgent Reviewer(IChatClient client, Transcript transcript) =>
        Specialist(client, transcript, "ReviewerAgent",
            "You provide advisory review of the exact supplied draft against the supplied fictional sources. " +
            "Flag unsupported assertions, missing citations, and missing disclaimers. " +
            "Approval is an opinion, never verification or a guarantee. You have no authority over host state.");

    public static AIAgent CreateMain(IChatClient client, Transcript transcript, OrderedWorkflow workflow) =>
        // Microsoft.Agents.AI.Harness supplies function dispatch, conversation
        // history, and the bounded tool loop; C# wrappers enforce phase legality.
        new TracingChatClient(client, "MainAgent44", transcript).AsHarnessAgent(new HarnessAgentOptions
        {
            Name = "MainAgent44",
            MaximumIterationsPerRequest = 12,
            DisableFileMemory = true,
            DisableTodoProvider = true,
            DisableAgentSkillsProvider = true,
            DisableAgentModeProvider = true,
            DisableWebSearch = true,
            DisableToolAutoApproval = true,
            DisableCompaction = true,
            ChatOptions = new ChatOptions
            {
                Tools = workflow.CreateTools().ToList(),
                AllowMultipleToolCalls = false,
                MaxOutputTokens = 1600,
                Instructions = """
                    Coordinate this ordered educational workflow using the supplied tools:
                    research_sources -> write_draft -> review_draft.
                    If feedback or host checks warrant it, call revise_draft ONCE then review_draft once more.
                    Then call finish_report. Never skip dependencies or keep retrying rejected calls.
                    Only one revision (two drafts and two reviews total) is allowed.
                    If checks cannot pass, explain that the workflow is incomplete.
                    Report actual tool results, not imagined outputs. Reviewer feedback is advisory.
                    Structure checks do not verify assertions or guarantee correctness.
                    This is fictional mock research, not financial advice.
                    """
            }
        });

    private static AIAgent Specialist(IChatClient client, Transcript transcript, string name, string instructions) =>
        // Microsoft.Agents.AI.AsAIAgent supplies standard request/response and
        // session handling. No tools means these roles cannot change host state.
        new TracingChatClient(client, name, transcript).AsAIAgent(new ChatClientAgentOptions
        {
            Name = name,
            ChatOptions = new ChatOptions { Instructions = instructions, MaxOutputTokens = 1400 }
        });
}
