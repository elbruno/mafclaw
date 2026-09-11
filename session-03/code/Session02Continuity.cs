internal sealed class Session02Continuity
{
    public string WorkingFolder { get; } = Path.Combine(AppContext.BaseDirectory, "working");
    public string FileAccess { get; } = "portfolio and reports stay under the approved working folder";
    public string Approval { get; } = "sensitive side effects require an explicit human decision";
    public string Memory { get; } = "user-scoped preferences are deliberate and inspectable";

    public void EnsureWorkingFolder()
    {
        Directory.CreateDirectory(WorkingFolder);
    }
}
