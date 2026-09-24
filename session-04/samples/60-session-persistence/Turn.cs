// Objective: keep a plain, LLM-free record of what "session state" actually is.
// A. Represent one turn as an explicit, serializable value.
namespace MafClaw.Session04.Samples;

public sealed record Turn(string Role, string Text);
