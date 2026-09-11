using System.Text.Json;

// A. Load named skills.
// B. Select the skill needed for the mock portfolio brief.
// C. Show that a description is not permission.
var skills = JsonSerializer.Deserialize<List<Skill>>("""
[
  { "name": "portfolio-check", "purpose": "Summarize the mock watchlist." },
  { "name": "workspace-scan", "purpose": "Run one approved workspace check." },
  { "name": "background-research", "purpose": "Queue a follow-up note." }
]
""", new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
    ?? throw new InvalidOperationException("Skill catalog was empty.");

var selected = skills.Single(skill => skill.Name == "portfolio-check");
Console.WriteLine("Sample 10 - plain skill catalog");
Console.WriteLine($"Selected: {selected.Name}");
Console.WriteLine($"Purpose: {selected.Purpose}");
Console.WriteLine("The host still decides which implementation is allowed.");

internal sealed record Skill(string Name, string Purpose);
