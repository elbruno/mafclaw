using System.Globalization;

var sandboxRoot = Path.Combine(AppContext.BaseDirectory, "working");
Directory.CreateDirectory(sandboxRoot);
var portfolioPath = Path.Combine(sandboxRoot, "portfolio.csv");
File.WriteAllText(portfolioPath, "symbol,shares,average_cost\nMSFT,25,430.10\nSPY,40,530.25\nNVDA,18,142.50\n");

Console.WriteLine("Session 2 sample: safe file access");
Console.WriteLine("The agent may only read files inside the sandbox.");

var allowedContent = ReadWithinSandbox(portfolioPath, sandboxRoot);
Console.WriteLine($"Allowed file read: {portfolioPath}");
Console.WriteLine(allowedContent);

var unsafePath = Path.Combine(Path.GetTempPath(), "outside-the-sandbox.txt");
File.WriteAllText(unsafePath, "not allowed");
Console.WriteLine();
Console.WriteLine("Unsafe path check: ");
Console.WriteLine(IsSafePath(unsafePath, sandboxRoot) ? "Allowed" : "Blocked");

static string ReadWithinSandbox(string path, string sandboxRoot)
{
    if (!IsSafePath(path, sandboxRoot))
    {
        throw new InvalidOperationException("This path is outside the approved working folder.");
    }

    return File.ReadAllText(path);
}

static bool IsSafePath(string candidatePath, string sandboxRoot)
{
    var fullCandidate = Path.GetFullPath(candidatePath);
    var fullRoot = Path.GetFullPath(sandboxRoot);
    return fullCandidate.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
}
