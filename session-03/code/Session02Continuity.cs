// Objective: carry Session 02 trust boundaries into the Session 03 composition.
// Steps:
// A. Name the approved working folder.
// B. Describe the file, approval, and memory boundaries.
// C. Create the folder before sample work uses it.

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
