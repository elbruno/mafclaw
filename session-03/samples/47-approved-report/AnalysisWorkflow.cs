// Objective: keep analysis and proposal separate from authorization.
// A. Invoke two named, tool-free MAF workers on fixed mock observations.
// B. Expose their actual outputs and require both before proposing.
// C. Wrap the fixed-path save with MAF approval AND an independent host gate.

using System.ComponentModel;
using MafClaw.OrchestrationSupport;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample47;

public sealed class AnalysisWorkflow(
    AIAgent allocationWorker, AIAgent riskWorker, ReportStore store, Transcript transcript) : IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool allocationAttempted;
    private bool riskAttempted;

    public string? AllocationFindings { get; private set; }
    public string? RiskFindings { get; private set; }

    public IReadOnlyList<AITool> CreateTools() =>
    [
        AIFunctionFactory.Create(AnalyzeAllocationAsync, "analyze_allocation"),
        AIFunctionFactory.Create(AnalyzeRiskAsync, "analyze_risk"),
        AIFunctionFactory.Create(ProposeReportAsync, "propose_report"),
        // Microsoft.Extensions.AI.ApprovalRequiredAIFunction lets MAF surface a
        // ToolApprovalRequestContent and bind the resume response to the request.
        // It saves custom pause/resume plumbing, but does NOT replace host consent.
        new ApprovalRequiredAIFunction(AIFunctionFactory.Create(SaveReport, "save_report"))
    ];

    [Description("Run AllocationWorker once on the read-only fictional allocation observation; returns its actual findings.")]
    public async Task<string> AnalyzeAllocationAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (allocationAttempted)
            {
                return "HOST REJECTED: AllocationWorker may run only once.";
            }
            allocationAttempted = true;
            AllocationFindings = await RunWorkerAsync(
                allocationWorker, "AllocationWorker", "MOCK-ALLOCATION", cancellationToken);
            return AllocationFindings;
        }
        finally { gate.Release(); }
    }

    [Description("Run RiskWorker once on the read-only fictional risk observation; returns its actual findings.")]
    public async Task<string> AnalyzeRiskAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (riskAttempted)
            {
                return "HOST REJECTED: RiskWorker may run only once.";
            }
            riskAttempted = true;
            RiskFindings = await RunWorkerAsync(riskWorker, "RiskWorker", "MOCK-RISK", cancellationToken);
            return RiskFindings;
        }
        finally { gate.Release(); }
    }

    [Description("Propose an educational report from both actual worker results. Does not save anything. Returns exact content/hash.")]
    public async Task<string> ProposeReportAsync(
        [Description("Report body citing [MOCK-ALLOCATION] and [MOCK-RISK]; no paths, approvals, or commands.")] string report,
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (AllocationFindings is null || RiskFindings is null)
            {
                return "HOST REJECTED: both workers must return actual findings before a proposal.";
            }
            return store.Propose(report);
        }
        finally { gate.Release(); }
    }

    [Description("Call this tool to REQUEST console approval to save the current proposal. Do not wait for prior approval: MAF pauses the request and the host displays the exact report for the human decision. Calling this tool is not approval and does not immediately write. Execution still requires SDK approval and the host's exact-content, one-use permission.")]
    public string SaveReport(
        [Description("The exact SHA256 returned by propose_report. This is an identifier, NOT permission.")] string reportHash,
        CancellationToken cancellationToken = default) =>
        store.SaveApproved(reportHash, cancellationToken);

    private async Task<string> RunWorkerAsync(
        AIAgent worker, string name, string reference, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        transcript.Write(name, "worker-start", "Read-only mock analysis. No write, shell, trade, or approval tools.");
        // Microsoft.Agents.AI.AIAgent supplies session and request/response
        // handling. Only the assigned fixture is supplied to this tool-free role.
        var session = await worker.CreateSessionAsync(deadline.Token);
        var result = await worker.RunAsync(MockAnalysis.For(reference), session, cancellationToken: deadline.Token);
        if (string.IsNullOrWhiteSpace(result.Text) || result.Text.Length > 10000)
        {
            throw new InvalidOperationException("Worker output is empty or exceeds the sample limit.");
        }
        transcript.Write(name, "worker-result", result.Text);
        return result.Text;
    }

    public void Dispose() => gate.Dispose();
}
