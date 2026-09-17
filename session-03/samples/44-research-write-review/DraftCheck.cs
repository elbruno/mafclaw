// Objective: separate deterministic structure checks from model critique.
// A. Require every known citation and the educational disclaimer.
// B. Reject unknown mock citation identifiers.
// C. Never treat these checks or reviewer approval as factual verification.

using System.Text.RegularExpressions;

namespace MafClaw.Sample44;

public sealed record DraftCheck(bool RequiredReferences, bool Disclaimer, bool OnlyKnownReferences)
{
    public const string RequiredDisclaimer = "Educational mock report. Not financial advice.";
    public bool Passed => RequiredReferences && Disclaimer && OnlyKnownReferences;
    public string Summary =>
        $"HOST STRUCTURE: references={RequiredReferences}; disclaimer={Disclaimer}; known-reference-ids={OnlyKnownReferences}; " +
        "assertions remain UNVERIFIED; reviewer feedback is advisory.";

    public static DraftCheck Inspect(string draft, IReadOnlyList<ResearchSource> sources)
    {
        var ids = sources.Select(source => source.Id).ToHashSet(StringComparer.Ordinal);
        var referenced = Regex.Matches(draft, @"\[(MOCK-[A-Za-z0-9-]+)\]")
            .Select(match => match.Groups[1].Value);
        return new DraftCheck(
            ids.All(id => draft.Contains($"[{id}]", StringComparison.Ordinal)),
            draft.Contains(RequiredDisclaimer, StringComparison.OrdinalIgnoreCase),
            referenced.All(ids.Contains));
    }
}
