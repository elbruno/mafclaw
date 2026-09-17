// Objective: keep validation and execution tied to the same command specification.
// Steps:
// A. Store the host-selected executable and arguments.
// B. Match the complete request without parsing shell syntax.
// C. Keep the same matching specification available to the launcher.

namespace MafClaw.Sample20;

internal sealed record CommandSpec(string FileName, IReadOnlyList<string> Arguments)
{
    public bool Matches(IReadOnlyList<string> request) =>
        request.Count == Arguments.Count + 1 &&
        StringComparer.Ordinal.Equals(request[0], FileName) &&
        request.Skip(1).SequenceEqual(Arguments, StringComparer.Ordinal);
}
