// Objective: define one JSON-friendly transcript entry for Sample 60 (plain C#).
// A. Record who spoke and the text they supplied.
// B. Let System.Text.Json save/load these values as part of the transcript list.

namespace MafClaw.Session04.Samples;

// No model memory is hidden here: Program.cs must explicitly keep, save and reload each entry.
public sealed record Turn(string Role, string Text);
