// Objective: retain a trusted, in-memory baseline for one synthetic confirmation.
// Steps:
// A. Record the original filename and the expected final name.
// B. Record the exact byte length and full SHA-256 hash written by the host.
// C. Use this baseline for verification, never for performing the rename.

namespace MafClaw.Sample21;

internal sealed record DemoFile(
    string OriginalName, string ExpectedName, long Length, string ContentHash);
