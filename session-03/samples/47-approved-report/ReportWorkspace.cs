// Objective: confine this mock report to a unique current-user run folder.
// A. Pick the run identifier in the host, never from model arguments.
// B. Use one fixed output filename and preserve earlier runs.
// C. Reject existing reparse-point ancestors before any report creation.

namespace MafClaw.Sample47;

public sealed class ReportWorkspace
{
    public const string OutputName = "educational-report.md";

    public ReportWorkspace(string trustedHostRoot)
    {
        var root = Path.GetFullPath(trustedHostRoot);
        DirectoryPath = Path.Combine(root, "current-user",
            $"run-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
        OutputPath = Path.Combine(DirectoryPath, OutputName);
    }

    public string DirectoryPath { get; }
    public string OutputPath { get; }

    public void PrepareForApprovedWrite()
    {
        // This is filesystem scope checking, not an OS sandbox against a hostile
        // process running as the same user. No tool accepts a path from the model.
        RejectReparseAncestors();
        Directory.CreateDirectory(DirectoryPath);
        RejectReparseAncestors();
        if (!string.Equals(Path.GetDirectoryName(OutputPath), DirectoryPath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Report path is outside the mock working directory.");
        }
    }

    public void RejectReparseAncestors()
    {
        for (var current = new DirectoryInfo(DirectoryPath); current is not null; current = current.Parent)
        {
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException("Linked report directories are not permitted.");
            }
        }
    }
}
