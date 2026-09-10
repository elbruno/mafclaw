// Session flow:
// A. Keep one fixed file path for the current demo user.
// B. Load existing profile facts before each operation.
// C. Save new facts with an atomic file replacement.
// D. Return only the current user's memory to callers.

using System.Text.Json;

internal sealed class LocalFileMemoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly string memoryPath;
    private readonly string scope;

    public LocalFileMemoryStore(string memoryRoot, string scope)
    {
        if (string.IsNullOrWhiteSpace(scope) ||
            scope.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            !Path.GetFileName(scope).Equals(scope, StringComparison.Ordinal))
        {
            throw new ArgumentException("Memory scope must be a safe file name.", nameof(scope));
        }

        Directory.CreateDirectory(memoryRoot);
        this.scope = scope;
        memoryPath = Path.Combine(memoryRoot, $"{scope}.json");
    }

    public string MemoryPath => memoryPath;

    public async Task<string> SaveProfileFactAsync(string fact, CancellationToken cancellationToken = default)
    {
        var normalizedFact = fact.Trim();
        if (string.IsNullOrWhiteSpace(normalizedFact))
        {
            return "Denied: the profile fact cannot be empty.";
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            var memory = await LoadRecordAsync(cancellationToken);
            var factAdded = !memory.ProfileFacts.Contains(normalizedFact, StringComparer.OrdinalIgnoreCase);
            if (factAdded)
            {
                memory.ProfileFacts.Add(normalizedFact);
            }

            var updated = memory with { UpdatedAtUtc = DateTimeOffset.UtcNow };
            var json = JsonSerializer.Serialize(updated, JsonOptions);
            var temporaryPath = $"{memoryPath}.tmp";

            await File.WriteAllTextAsync(temporaryPath, json, cancellationToken);
            File.Move(temporaryPath, memoryPath, overwrite: true);

            return factAdded
                ? $"Saved current-user profile memory to {memoryPath}"
                : $"Current-user profile memory already contains that fact in {memoryPath}";
        }
        catch (InvalidDataException ex)
        {
            return $"Local memory error: {ex.Message}";
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<string> RecallProfileAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var memory = await LoadRecordAsync(cancellationToken);
            return memory.ProfileFacts.Count == 0
                ? "No current-user profile memory has been saved yet."
                : string.Join(Environment.NewLine, memory.ProfileFacts.Select(fact => $"- {fact}"));
        }
        catch (InvalidDataException ex)
        {
            return $"Local memory error: {ex.Message}";
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<string> ReadMemoryFileAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(memoryPath))
        {
            return "No local memory file has been created yet.";
        }

        return await File.ReadAllTextAsync(memoryPath, cancellationToken);
    }

    private async Task<LocalMemoryRecord> LoadRecordAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(memoryPath))
        {
            return new LocalMemoryRecord(scope, [], DateTimeOffset.UtcNow);
        }

        var json = await File.ReadAllTextAsync(memoryPath, cancellationToken);
        LocalMemoryRecord memory;
        try
        {
            memory = JsonSerializer.Deserialize<LocalMemoryRecord>(json)
                ?? throw new InvalidDataException($"Local memory file is empty or invalid: {memoryPath}");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Local memory file contains invalid JSON: {memoryPath}", ex);
        }

        if (!string.Equals(memory.Scope, scope, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Local memory scope does not match the configured scope: {memoryPath}");
        }

        if (memory.ProfileFacts is null)
        {
            throw new InvalidDataException($"Local memory profile facts are missing: {memoryPath}");
        }

        return memory;
    }
}
