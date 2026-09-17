// Objective: verify the real filesystem instead of trusting the model's summary.
// Steps:
// A. Inspect only the current demo directory, rejecting unexpected entries and links.
// B. Compare every expected name and file hash with the host's in-memory baseline.
// C. Report verified completion or explicit pending/failed checks without modifying files.

using System.Security.Cryptography;

namespace MafClaw.Sample21;

internal static class DemoVerifier
{
    public static bool Verify(DemoWorkspace workspace, TextWriter output)
    {
        var problems = new List<string>();
        var renamed = 0;
        try
        {
            // A. Do not follow links or read the contents of unexpected files.
            var directory = new DirectoryInfo(workspace.DirectoryPath);
            if (!directory.Exists || (directory.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                output.WriteLine("HOST CHECK: NOT COMPLETE (workspace is missing or is a link).");
                return false;
            }
            var entries = directory.EnumerateFileSystemInfos()
                .ToDictionary(entry => entry.Name, StringComparer.Ordinal);
            var knownNames = workspace.Files
                .SelectMany(file => new[] { file.OriginalName, file.ExpectedName })
                .ToHashSet(StringComparer.Ordinal);
            foreach (var entry in entries.Values)
            {
                if (!knownNames.Contains(entry.Name))
                {
                    problems.Add($"Unexpected entry: {entry.Name}");
                }
            }

            // B. Exactly one original/final filename must exist for each known file.
            foreach (var baseline in workspace.Files)
            {
                var names = new[] { baseline.OriginalName, baseline.ExpectedName }
                    .Where(entries.ContainsKey).ToArray();
                if (names.Length != 1)
                {
                    problems.Add($"Missing or duplicate confirmation: {baseline.OriginalName}");
                    continue;
                }

                var entry = entries[names[0]];
                if (entry is not FileInfo file ||
                    (entry.Attributes & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
                {
                    problems.Add($"Not a regular demo file: {entry.Name}");
                    continue;
                }

                if (file.Length != baseline.Length)
                {
                    problems.Add($"Content length changed: {file.Name}");
                    continue;
                }
                using var stream = file.OpenRead();
                var hash = Convert.ToHexString(SHA256.HashData(stream));
                output.WriteLine($"  AFTER: {file.Name} | SHA-256 {hash[..12]}...");
                if (hash != baseline.ContentHash)
                {
                    problems.Add($"Content changed (SHA-256): {file.Name}");
                }
                if (file.Name == baseline.ExpectedName)
                {
                    renamed++;
                }
                else
                {
                    problems.Add($"Rename pending: {file.Name} -> {baseline.ExpectedName}");
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            problems.Add($"Could not inspect the demo workspace ({exception.GetType().Name}).");
        }

        // C. A successful shell exit or an assistant claim cannot produce this verdict.
        if (problems.Count == 0 && renamed == workspace.Files.Count)
        {
            output.WriteLine($"HOST VERIFIED: {renamed}/{workspace.Files.Count} expected filenames; original SHA-256 hashes match.");
            return true;
        }

        output.WriteLine($"HOST CHECK: NOT COMPLETE ({renamed}/{workspace.Files.Count} expected filenames).");
        foreach (var problem in problems)
        {
            output.WriteLine($"  {problem}");
        }
        return false;
    }
}
