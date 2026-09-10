// Session flow:
// A. Create the approved working folder and seed mock portfolio data.
// B. Read the portfolio only when the path stays inside that folder.
// C. Try a file outside the folder to demonstrate the guard.
// D. Print the allowed and blocked results for the audience.

using System.Globalization;

// Create a local sandbox that represents the only approved file area.
var sandboxRoot = Path.Combine(AppContext.BaseDirectory, "working");
Directory.CreateDirectory(sandboxRoot);

// Seed a predictable portfolio file so the demo output is stable.
var portfolioPath = Path.Combine(sandboxRoot, "portfolio.csv");
File.WriteAllText(portfolioPath, "symbol,shares,average_cost\nMSFT,25,430.10\nSPY,40,530.25\nNVDA,18,142.50\n");

Console.WriteLine("Session 2 sample 10: safe file access");
Console.WriteLine("The agent may only read files inside the sandbox.");

// This read succeeds because the file is inside the approved root.
var allowedContent = ReadWithinSandbox(portfolioPath, sandboxRoot);
Console.WriteLine($"Allowed file read: {portfolioPath}");
Console.WriteLine(allowedContent);

// This path is outside the sandbox, so the guard should block it.
var unsafePath = Path.Combine(Path.GetTempPath(), "outside-the-sandbox.txt");
File.WriteAllText(unsafePath, "not allowed");
Console.WriteLine();
Console.WriteLine("Unsafe path check: ");
Console.WriteLine(IsSafePath(unsafePath, sandboxRoot) ? "Allowed" : "Blocked");

static string ReadWithinSandbox(string path, string sandboxRoot)
{
    // Every file operation goes through the same boundary check.
    if (!IsSafePath(path, sandboxRoot))
    {
        throw new InvalidOperationException("This path is outside the approved working folder.");
    }

    return File.ReadAllText(path);
}

static bool IsSafePath(string candidatePath, string sandboxRoot)
{
    // Compare full paths so relative paths cannot escape the sandbox.
    var fullCandidate = Path.GetFullPath(candidatePath);
    var fullRoot = Path.GetFullPath(sandboxRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    var fullRootWithSeparator = fullRoot + Path.DirectorySeparatorChar;

    return string.Equals(fullCandidate, fullRoot, StringComparison.OrdinalIgnoreCase)
        || fullCandidate.StartsWith(fullRootWithSeparator, StringComparison.OrdinalIgnoreCase);
}
