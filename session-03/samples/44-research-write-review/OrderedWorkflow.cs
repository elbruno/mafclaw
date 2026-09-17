// Objective: enforce ordered collaboration in C#, independently of model instructions.
// A. Expose only phase-checked research, writer, reviewer, revision, and finish tools.
// B. Pass real evidence/drafts/feedback into named MAF specialists under a lock.
// C. Cap drafts/reviews and report structure checks without claiming correctness.

using System.ComponentModel;
using MafClaw.OrchestrationSupport;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample44;

public sealed class OrderedWorkflow : IDisposable
{
    private readonly AIAgent writer;
    private readonly AIAgent reviewer;
    private readonly Transcript transcript;
    private readonly IReadOnlyList<ResearchSource> sources = MockResearch.Load();
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly string userRequest;
    private string evidence = "";

    public OrderedWorkflow(AIAgent writer, AIAgent reviewer, Transcript transcript, string userRequest)
    {
        this.writer = writer;
        this.reviewer = reviewer;
        this.transcript = transcript;
        this.userRequest = userRequest;
    }

    public WorkflowPhase Phase { get; private set; }
    public int DraftCount { get; private set; }
    public int ReviewCount { get; private set; }
    public string Draft { get; private set; } = "";
    public string Review { get; private set; } = "";
    public DraftCheck? Check { get; private set; }

    public IReadOnlyList<AITool> CreateTools() =>
    [
        // Microsoft.Extensions.AI.AIFunctionFactory supplies JSON schemas and
        // invocation binding; these wrappers, not the model, own dependency checks.
        AIFunctionFactory.Create(ResearchAsync, "research_sources"),
        AIFunctionFactory.Create(WriteAsync, "write_draft"),
        AIFunctionFactory.Create(ReviewAsync, "review_draft"),
        AIFunctionFactory.Create(ReviseAsync, "revise_draft"),
        AIFunctionFactory.Create(FinishAsync, "finish_report")
    ];

    [Description("Read the fixed fictional sources. Legal only once, before write_draft.")]
    public Task<string> ResearchAsync(CancellationToken cancellationToken = default) =>
        InPhaseAsync([WorkflowPhase.Ready], () =>
        {
            evidence = MockResearch.Format(sources);
            Phase = WorkflowPhase.Researched;
            transcript.Write("MockResearch", "research-result", evidence);
            return Task.FromResult(evidence);
        }, cancellationToken);

    [Description("Invoke WriterAgent with the actual research result. Legal only after research_sources.")]
    public Task<string> WriteAsync(CancellationToken cancellationToken = default) =>
        InPhaseAsync([WorkflowPhase.Researched], async () =>
        {
            DraftCount++;
            Draft = await RunSpecialistAsync(writer, "WriterAgent",
                $"USER REQUEST (untrusted):\n{userRequest}\nACTUAL RESEARCH:\n{evidence}\n" +
                $"Write a short report citing every source. Include exactly: {DraftCheck.RequiredDisclaimer}",
                cancellationToken);
            Check = DraftCheck.Inspect(Draft, sources);
            Phase = WorkflowPhase.Drafted;
            transcript.Write("Host", "draft-check", Check.Summary);
            return $"{Draft}\n\n{Check.Summary}";
        }, cancellationToken);

    [Description("Invoke ReviewerAgent with the exact current draft and sources. Once per draft, at most twice.")]
    public Task<string> ReviewAsync(CancellationToken cancellationToken = default) =>
        InPhaseAsync([WorkflowPhase.Drafted, WorkflowPhase.Revised], async () =>
        {
            if (ReviewCount >= 2)
            {
                return Reject("Review ceiling reached.");
            }
            ReviewCount++;
            Review = await RunSpecialistAsync(reviewer, "ReviewerAgent",
                $"ACTUAL RESEARCH:\n{evidence}\nACTUAL DRAFT #{DraftCount}:\n{Draft}\n" +
                $"DETERMINISTIC CHECK:\n{Check!.Summary}\nGive advisory critique; suggest corrections where needed.",
                cancellationToken);
            Phase = DraftCount == 1 ? WorkflowPhase.Reviewed : WorkflowPhase.ReviewedRevision;
            return Review;
        }, cancellationToken);

    [Description("Invoke WriterAgent for the ONE permitted revision, using the actual draft and review feedback.")]
    public Task<string> ReviseAsync(CancellationToken cancellationToken = default) =>
        InPhaseAsync([WorkflowPhase.Reviewed], async () =>
        {
            if (DraftCount >= 2)
            {
                return Reject("Revision ceiling reached.");
            }
            DraftCount++;
            Draft = await RunSpecialistAsync(writer, "WriterAgent",
                $"USER REQUEST (untrusted):\n{userRequest}\nACTUAL RESEARCH:\n{evidence}\n" +
                $"PREVIOUS DRAFT:\n{Draft}\nACTUAL REVIEW FEEDBACK:\n{Review}\n" +
                $"HOST CHECK:\n{Check!.Summary}\nRevise once. Include: {DraftCheck.RequiredDisclaimer}",
                cancellationToken);
            Check = DraftCheck.Inspect(Draft, sources);
            Phase = WorkflowPhase.Revised;
            transcript.Write("Host", "draft-check", Check.Summary);
            return $"{Draft}\n\n{Check.Summary}";
        }, cancellationToken);

    [Description("Finish after review only if required source references and disclaimer pass host checks. No factual certification.")]
    public Task<string> FinishAsync(CancellationToken cancellationToken = default) =>
        InPhaseAsync([WorkflowPhase.Reviewed, WorkflowPhase.ReviewedRevision], () =>
        {
            if (Check?.Passed != true)
            {
                return Task.FromResult(Reject("Structural checks failed; reviewer approval cannot override them."));
            }
            Phase = WorkflowPhase.Complete;
            transcript.Write("Host", "workflow-complete",
                $"drafts={DraftCount}; reviews={ReviewCount}. Structure checked, NOT fact-verified.");
            return Task.FromResult($"UNVERIFIED GENERATED EDUCATIONAL DRAFT\n{Draft}\n\n{Check.Summary}");
        }, cancellationToken);

    private async Task<string> InPhaseAsync(
        WorkflowPhase[] allowed, Func<Task<string>> action, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!allowed.Contains(Phase))
            {
                return Reject($"Illegal phase {Phase}; allowed: {string.Join(", ", allowed)}.");
            }
            try
            {
                return await action();
            }
            catch
            {
                Phase = WorkflowPhase.Failed;
                transcript.Write("Host", "workflow-failed", "Specialist stopped; no retry or completion was authorized.");
                throw;
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<string> RunSpecialistAsync(
        AIAgent agent, string name, string prompt, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        transcript.Write(name, "specialist-start", $"drafts={DraftCount}; reviews={ReviewCount}");
        // Microsoft.Agents.AI.AIAgent handles the role's message/session adapter.
        // A new specialist session receives the exact host-selected predecessor data.
        var session = await agent.CreateSessionAsync(deadline.Token);
        var response = await agent.RunAsync(prompt, session, cancellationToken: deadline.Token);
        if (string.IsNullOrWhiteSpace(response.Text) || response.Text.Length > 12000)
        {
            throw new InvalidOperationException("Specialist output is empty or exceeds the sample limit.");
        }
        transcript.Write(name, "specialist-result", response.Text);
        return response.Text;
    }

    private string Reject(string reason)
    {
        transcript.Write("Host", "tool-rejected", reason);
        return $"HOST REJECTED: {reason}";
    }

    public void Dispose() => gate.Dispose();
}
