internal sealed record LocalMemoryRecord(
    string Scope,
    List<string> ProfileFacts,
    DateTimeOffset UpdatedAtUtc);
