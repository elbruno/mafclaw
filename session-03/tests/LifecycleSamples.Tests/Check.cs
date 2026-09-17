// Objective: keep executable tests focused on lifecycle assertions.
// A. Check facts. B. Bound watchdog waits, not worker behavior. C. Test rejected operations.
namespace MafClaw.LifecycleSamples.Tests;

internal static class Check
{
    public static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    public static void That(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static async Task Within(Task task) => await task.WaitAsync(TimeSpan.FromSeconds(10));

    public static async Task<T> Within<T>(Task<T> task) => await task.WaitAsync(TimeSpan.FromSeconds(10));

    public static void Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
