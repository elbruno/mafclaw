using System.Text.Json;

var settings = Session03Settings.Load();
var catalog = SkillCatalog.Load();
var watchlist = MockWatchlist.Load();
var session02 = new Session02Continuity();
session02.EnsureWorkingFolder();
var shellTool = new SafeShellTool(settings.Shell);
var backgroundTasks = new BackgroundTaskRunner(settings.Background);
var sample = new Session03Sample(settings, catalog, watchlist, session02, shellTool, backgroundTasks);

Console.WriteLine("mafclaw - Session 03");
Console.WriteLine("Offline orchestration sample. Mock data only. Not financial advice.");
Console.WriteLine();

var result = await sample.RunAsync();
Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    WriteIndented = true
}));
