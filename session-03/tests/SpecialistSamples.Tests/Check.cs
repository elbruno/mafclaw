// Objective: provide a small dependency-free assertion surface.
// A. Assert conditions with focused explanations.
// B. Compare deterministic expected values.
// C. Fail fast without hiding test errors.

namespace MafClaw.SpecialistSamples.Tests;

public static class Check
{
    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void Equal<T>(T expected, T actual, string message) =>
        True(EqualityComparer<T>.Default.Equals(expected, actual),
            $"{message}: expected {expected}, actual {actual}");
}
