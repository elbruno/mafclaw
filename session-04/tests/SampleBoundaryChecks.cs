// Objective: prevent the introductory Session 4 samples from depending on the final application.
// A. Find the session source root from the running test output.
// B. Reject project references that escape the samples tree.
// C. Reject final-app symbols in teaching source and require purpose headers.

using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MafClaw.Session04.Tests;

internal static class SampleBoundaryChecks
{
    public static void Run()
    {
        // A. Source checks belong to repository verification, not a deployed runtime health check.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MafClaw.Session04.slnx")))
            directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Session 4 source root was not found.");
        var samples = Path.Combine(directory.FullName, "samples");

        // B. A sample may share non-domain helpers under samples, but never a code/ project.
        foreach (var project in Directory.EnumerateFiles(samples, "*.csproj", SearchOption.AllDirectories))
        {
            foreach (var reference in XDocument.Load(project).Descendants("ProjectReference"))
            {
                var include = reference.Attribute("Include")?.Value
                    ?? throw new InvalidOperationException("Project reference has no Include path.");
                var target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project)!,
                    include.Replace('\\', Path.DirectorySeparatorChar)));
                var relative = Path.GetRelativePath(samples, target);
                if (Path.IsPathRooted(relative) || relative == ".." ||
                    relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Sample depends on the final app or outside source: {project}");
            }
        }

        // C. Skip generated output; inspect teaching files rather than assembly metadata.
        foreach (var file in Directory.EnumerateFiles(samples, "*.cs", SearchOption.AllDirectories))
        {
            var parts = Path.GetRelativePath(samples, file).Split(Path.DirectorySeparatorChar);
            if (parts.Any(part => part is "bin" or "obj" or ".local" or "out")) continue;
            var source = File.ReadAllText(file);
            if (!source.StartsWith("// Objective:", StringComparison.Ordinal) ||
                Regex.IsMatch(source, @"\b(?:Finance\w*|MockPortfolio|SafeErrors)\b"))
                throw new InvalidOperationException($"Missing teaching header or final-app dependency: {file}");
        }
    }
}
