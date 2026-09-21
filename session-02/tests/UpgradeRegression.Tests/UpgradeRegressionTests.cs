// Objective: protect approval retry/deadline and local-memory behavior across runtime upgrades.
// A. Inject bounded approval input and assert fail-closed results.
// B. Verify persistence, duplicate handling, and current-user scope checks.
// C. Clean only this run's synthetic files and propagate every failed assertion.

internal static class UpgradeRegressionTests
{
    public static async Task<int> RunAsync()
    {
        var passed = 0;
        var root = Path.Combine(AppContext.BaseDirectory, "test-artifacts", Guid.NewGuid().ToString("N"));
        try
        {
            // A. Exercise the sample's actual policy, not a reimplementation.
            foreach (var input in new[] { "y", "yes", " Y " })
            {
                var result = await Policy([input]).RequestApprovalAsync("mock_trade", "{}");
                Check(result.Approved, "explicit approval", ref passed);
            }
            Check(!(await Policy(["n"]).RequestApprovalAsync("mock_trade", "{}")).Approved, "explicit denial", ref passed);
            Check((await Policy(["invalid", "y"]).RequestApprovalAsync("mock_trade", "{}")).Approved, "retry then approve", ref passed);
            var timedOut = await Policy([null, null, null]).RequestApprovalAsync("mock_trade", "{}");
            Check(!timedOut.Approved && timedOut.Message.Contains("3"), "timeout attempts fail closed", ref passed);
            Check(!(await Policy(["invalid", "", "wrong"]).RequestApprovalAsync("mock_trade", "{}")).Approved,
                "invalid attempts fail closed", ref passed);
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            var cancellationPolicy = new TimedApprovalPolicy(3, TimeSpan.FromSeconds(1),
                (_, token) => Task.FromCanceled<string?>(token));
            Check(!(await cancellationPolicy.RequestApprovalAsync("mock_trade", "{}", cancelled.Token)).Approved,
                "cancellation fails closed", ref passed);

            // B. A fresh run-owned folder preserves all existing demo memory.
            var store = new LocalFileMemoryStore(root, "current-user");
            Check((await store.SaveProfileFactAsync("")).StartsWith("Denied:"), "empty fact denied", ref passed);
            Check(!File.Exists(store.MemoryPath), "denial has no file side effect", ref passed);
            await store.SaveProfileFactAsync("Synthetic classroom preference");
            var restarted = new LocalFileMemoryStore(root, "current-user");
            Check((await restarted.RecallProfileAsync()).Contains("Synthetic classroom preference"),
                "memory survives restart", ref passed);
            Check((await restarted.SaveProfileFactAsync("SYNTHETIC CLASSROOM PREFERENCE")).Contains("already contains"),
                "duplicate facts are not appended", ref passed);
            var other = new LocalFileMemoryStore(root, "other-user");
            Check((await other.RecallProfileAsync()).StartsWith("No current-user"), "scope isolation", ref passed);
            File.Copy(store.MemoryPath, other.MemoryPath);
            Check((await other.RecallProfileAsync()).Contains("scope does not match"), "forged scope rejected", ref passed);
            await File.WriteAllTextAsync(store.MemoryPath, "{invalid");
            Check((await restarted.RecallProfileAsync()).Contains("invalid JSON"), "corrupt memory fails visibly", ref passed);
            Console.WriteLine($"{passed}/15 Session 2 upgrade checks passed. Offline; no live services used.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"FAIL: {exception.Message}");
            return 1;
        }
        finally
        {
            // C. Never clean a shared temporary folder or a previous run's state.
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static TimedApprovalPolicy Policy(string?[] inputs)
    {
        var queue = new Queue<string?>(inputs);
        return new TimedApprovalPolicy(3, TimeSpan.FromSeconds(1), (_, _) => Task.FromResult(queue.Dequeue()));
    }

    private static void Check(bool condition, string name, ref int passed)
    {
        if (!condition)
            throw new InvalidOperationException(name);
        passed++;
        Console.WriteLine($"PASS: {name}");
    }
}
