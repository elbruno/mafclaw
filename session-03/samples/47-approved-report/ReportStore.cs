// Objective: make console approval a one-use host capability, not model text.
// A. Freeze and hash the exact proposed UTF-8 report for review.
// B. Arm permission only after explicit console approval of that same version/hash.
// C. Consume permission before a fixed-path write and verify actual bytes separately.

using System.Security.Cryptography;
using System.Text;
using MafClaw.OrchestrationSupport;

namespace MafClaw.Sample47;

public sealed class ReportStore(ReportWorkspace workspace, Transcript transcript)
{
    public const string Label = "FICTIONAL EDUCATIONAL REPORT\nUNVERIFIED model-generated analysis.\nNot financial advice. No trading actions.\n\n";
    private readonly object gate = new();
    private ReportProposal? proposal;
    private string? approvedHash;
    private long approvedVersion;
    private bool decisionTaken;
    private bool writeAttempted;
    private bool denied;
    private long version;

    public ReportWorkspace Workspace => workspace;
    public ReportProposal? Proposal { get { lock (gate) { return proposal; } } }
    public bool Saved { get; private set; }
    public bool Denied { get { lock (gate) { return denied; } } }

    public static string Hash(string content) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));

    public string Propose(string body)
    {
        lock (gate)
        {
            // A changed/repeated proposal invalidates any earlier permission even
            // if it is rejected. Hash equality alone never revives consumed consent.
            approvedHash = null;
            if (writeAttempted || denied || version >= 2)
            {
                return Reject("This run is closed or has reached its two-proposal limit.");
            }
            if (string.IsNullOrWhiteSpace(body) || body.Length > 12000 ||
                !body.Contains("[MOCK-ALLOCATION]", StringComparison.Ordinal) ||
                !body.Contains("[MOCK-RISK]", StringComparison.Ordinal))
            {
                return Reject("Proposal must be 1-12000 characters and cite both fictional sources.");
            }
            var content = Label + body;
            proposal = new ReportProposal(content, Hash(content), ++version);
            transcript.Write("Host", "report-proposed", $"version={version}; SHA256={proposal.Sha256}\n{content}");
            return $"PROPOSED, NOT SAVED. SHA256={proposal.Sha256}\n{content}";
        }
    }

    public async Task<bool> ReviewOnConsoleAsync(
        string requestedHash, TextReader input, TextWriter output, CancellationToken cancellationToken = default)
    {
        ReportProposal snapshot;
        lock (gate)
        {
            if (decisionTaken || writeAttempted || denied || proposal is null ||
                !string.Equals(requestedHash, proposal.Sha256, StringComparison.Ordinal))
            {
                approvedHash = null;
                Reject("No fresh matching proposal is available for human review.");
                return false;
            }
            snapshot = proposal;
            decisionTaken = true;
        }

        // Only this console path can grant the host's permission. MAF approval
        // content by itself and statements in prompts/tool arguments cannot do so.
        output.WriteLine("\nEXACT REPORT FOR HUMAN REVIEW — unverified analysis:");
        output.WriteLine(snapshot.Content);
        output.WriteLine($"Fixed destination: {workspace.OutputPath}");
        output.WriteLine($"UTF-8 SHA-256: {snapshot.Sha256}");
        output.WriteLine($"To save THESE EXACT BYTES ONCE, type: APPROVE {snapshot.Sha256}");
        output.WriteLine("Any other response or EOF denies this run. No trading is performed.");
        var answer = await ConsoleInput.ReadLineAsync(input, cancellationToken);
        lock (gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (answer != $"APPROVE {snapshot.Sha256}" || proposal != snapshot || writeAttempted)
            {
                approvedHash = null;
                denied = true;
                transcript.Write("Host", "approval-denied",
                    answer is null ? "Input closed. No report write authorized." : "Denied or content changed. No report write authorized.");
                return false;
            }
            approvedHash = snapshot.Sha256;
            approvedVersion = snapshot.Version;
            transcript.Write("Host", "human-approved", $"Exact content/version approved once: {snapshot.Sha256}");
            return true;
        }
    }

    public string SaveApproved(string reportHash, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var snapshot = proposal;
            if (snapshot is null || denied || writeAttempted || approvedHash is null ||
                snapshot.Version != approvedVersion ||
                reportHash != approvedHash || snapshot.Sha256 != approvedHash ||
                Hash(snapshot.Content) != approvedHash)
            {
                return Reject("No unused human permission for these exact report bytes. Model approval claims are not authority.");
            }

            // Consume BEFORE any filesystem operation. Failure cannot replay the
            // permission, and FileMode.CreateNew cannot overwrite a previous file.
            writeAttempted = true;
            approvedHash = null;
            workspace.PrepareForApprovedWrite();
            var bytes = Encoding.UTF8.GetBytes(snapshot.Content);
            using (var stream = new FileStream(workspace.OutputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }
            Saved = true;
            transcript.Write("Host", "file-result", $"CREATED {ReportWorkspace.OutputName}; bytes={bytes.Length}; SHA256={snapshot.Sha256}");
            return $"HOST WRITE RESULT: created {ReportWorkspace.OutputName}. Independent verification is still required.";
        }
    }

    public bool VerifySavedFile()
    {
        lock (gate)
        {
            if (!Saved || proposal is null || !File.Exists(workspace.OutputPath))
            {
                transcript.Write("Host", "file-verification", "NOT SAVED. Narrative is not filesystem evidence.");
                return false;
            }
            workspace.RejectReparseAncestors();
            var attributes = File.GetAttributes(workspace.OutputPath);
            if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            {
                transcript.Write("Host", "file-verification", "FAILED: output is not a regular file.");
                return false;
            }
            // This fresh read, not a model or tool's success string, checks actual bytes.
            var actual = File.ReadAllBytes(workspace.OutputPath);
            var expected = Encoding.UTF8.GetBytes(proposal.Content);
            var valid = actual.AsSpan().SequenceEqual(expected) &&
                Convert.ToHexString(SHA256.HashData(actual)) == proposal.Sha256;
            transcript.Write("Host", "file-verification", valid
                ? $"HOST VERIFIED: exact UTF-8 bytes and SHA256={proposal.Sha256}; {workspace.OutputPath}"
                : "FAILED: saved bytes no longer match the reviewed report.");
            return valid;
        }
    }

    private string Reject(string reason)
    {
        transcript.Write("Host", "write-gate-rejected", reason);
        return $"HOST REJECTED: {reason}";
    }
}
