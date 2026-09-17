// Objective: create a repeatable four-file demo without erasing previous runs.
// Steps:
// A. Allocate a fresh run directory.
// B. Write the synthetic fixtures and retain their hashes outside the shell's files.
// C. Show the audience the original names and baseline fingerprints.

using System.Security.Cryptography;
using System.Text;

namespace MafClaw.Sample21;

internal sealed class DemoWorkspace
{
    private DemoWorkspace(string directoryPath, IReadOnlyList<DemoFile> files)
    {
        DirectoryPath = directoryPath;
        Files = files;
    }

    public string DirectoryPath { get; }
    public IReadOnlyList<DemoFile> Files { get; }

    public static DemoWorkspace Create(string rootDirectory)
    {
        // A. A new run gets a new folder; never reset or delete an earlier workspace.
        var path = Path.GetFullPath(Path.Combine(
            rootDirectory, $"run-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"));
        if (Directory.Exists(path))
        {
            throw new IOException("The new demo workspace already exists.");
        }
        Directory.CreateDirectory(path);

        // B. Expected names are test criteria, not a hard-coded renaming routine.
        (string Original, string Expected, string Contents)[] fixtures =
        [
            ("trade confirmation 1.txt", "MSFT_BUY_10_shares.txt", "MSFT BUY 10 shares - mock confirmation"),
            ("conf_AAPL.txt", "AAPL_SELL_5_shares.txt", "AAPL SELL 5 shares - mock confirmation"),
            ("copy of trade 3.txt", "NVDA_BUY_8_shares.txt", "NVDA BUY 8 shares - mock confirmation"),
            ("SPY sell.txt", "SPY_SELL_12_shares.txt", "SPY SELL 12 shares - mock confirmation")
        ];
        var files = new List<DemoFile>();
        foreach (var fixture in fixtures)
        {
            var bytes = Encoding.UTF8.GetBytes(fixture.Contents);
            using (var stream = new FileStream(
                Path.Combine(path, fixture.Original), FileMode.CreateNew, FileAccess.Write))
            {
                stream.Write(bytes);
            }
            files.Add(new DemoFile(fixture.Original, fixture.Expected, bytes.Length,
                Convert.ToHexString(SHA256.HashData(bytes))));
        }
        return new DemoWorkspace(path, files.AsReadOnly());
    }

    public void WriteBefore(TextWriter output)
    {
        // C. Short hashes aid the presentation; verification compares all 256 bits.
        output.WriteLine($"Workspace: {DirectoryPath}");
        output.WriteLine($"BEFORE: {Files.Count} mock confirmations. Earlier runs are preserved.");
        foreach (var file in Files)
        {
            output.WriteLine($"  {file.OriginalName} | SHA-256 {file.ContentHash[..12]}...");
        }
    }
}
