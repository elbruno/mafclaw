// Objective: check the demo's evidence and real SDK approval contract offline.
// Steps:
// A. Create only synthetic, test-owned workspaces.
// B. Exercise file verification and the actual MAF/PowerShell integration.
// C. Print every result and remove only this run's temporary test root.

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Tools.Shell;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample21.Tests;

internal static class Sample21Tests
{
    public static async Task<int> RunAsync()
    {
        var root = Directory.CreateTempSubdirectory("mafclaw-sample21-tests-");
        DemoWorkspace Fresh() => DemoWorkspace.Create(root.FullName);
        (string Name, Func<Task> Run)[] tests =
        [
            ("fresh runs preserve prior folders and contain four originals", () =>
            {
                var first = Fresh();
                RenameFixture(first);
                var second = Fresh();
                Check(first.DirectoryPath != second.DirectoryPath, "Run directories must differ.");
                Check(DemoVerifier.Verify(first, TextWriter.Null), "The new run altered the previous one.");
                Check(second.Files.Count == 4 && Directory.GetFiles(second.DirectoryPath).Length == 4, "Wrong fixture count.");
                Check(second.Files.All(file => File.Exists(Path.Combine(second.DirectoryPath, file.OriginalName))), "Missing original.");
                return Task.CompletedTask;
            }),
            ("unchanged originals are not reported as completed", () =>
            {
                Check(!DemoVerifier.Verify(Fresh(), TextWriter.Null), "Original names passed final verification.");
                return Task.CompletedTask;
            }),
            ("correct names and hashes pass host verification", () =>
            {
                var workspace = Fresh();
                RenameFixture(workspace);
                Check(DemoVerifier.Verify(workspace, TextWriter.Null), "Correct fixture failed verification.");
                return Task.CompletedTask;
            }),
            ("same-length content changes fail SHA-256 verification", () =>
            {
                var workspace = Fresh();
                RenameFixture(workspace);
                var path = Path.Combine(workspace.DirectoryPath, workspace.Files[0].ExpectedName);
                var bytes = File.ReadAllBytes(path);
                bytes[0] = bytes[0] == (byte)'X' ? (byte)'Y' : (byte)'X';
                File.WriteAllBytes(path, bytes);
                Check(!DemoVerifier.Verify(workspace, TextWriter.Null), "Changed bytes passed the hash check.");
                return Task.CompletedTask;
            }),
            ("missing confirmations fail verification", () =>
            {
                var workspace = Fresh();
                RenameFixture(workspace);
                File.Delete(Path.Combine(workspace.DirectoryPath, workspace.Files[0].ExpectedName));
                Check(!DemoVerifier.Verify(workspace, TextWriter.Null), "Missing file passed verification.");
                return Task.CompletedTask;
            }),
            ("duplicate original and renamed files fail verification", () =>
            {
                var workspace = Fresh();
                var file = workspace.Files[0];
                File.Copy(Path.Combine(workspace.DirectoryPath, file.OriginalName), Path.Combine(workspace.DirectoryPath, file.ExpectedName));
                Check(!DemoVerifier.Verify(workspace, TextWriter.Null), "Duplicate file passed verification.");
                return Task.CompletedTask;
            }),
            ("unexpected files and directories fail verification", () =>
            {
                var workspace = Fresh();
                RenameFixture(workspace);
                File.WriteAllText(Path.Combine(workspace.DirectoryPath, "unexpected.txt"), "synthetic extra");
                Directory.CreateDirectory(Path.Combine(workspace.DirectoryPath, "unexpected-folder"));
                Check(!DemoVerifier.Verify(workspace, TextWriter.Null), "Unexpected entries passed verification.");
                return Task.CompletedTask;
            }),
            ("directories cannot masquerade as expected files", () =>
            {
                var workspace = Fresh();
                RenameFixture(workspace);
                var path = Path.Combine(workspace.DirectoryPath, workspace.Files[0].ExpectedName);
                File.Delete(path);
                Directory.CreateDirectory(path);
                Check(!DemoVerifier.Verify(workspace, TextWriter.Null), "A directory passed as a normal file.");
                return Task.CompletedTask;
            }),
            ("built-in shell context reports the actual PowerShell environment", async () =>
            {
                var workspace = Fresh();
                await using var shell = CreateShell(workspace);
                var context = new ShellEnvironmentProvider(shell, new ShellEnvironmentProviderOptions
                {
                    OverrideFamily = ShellFamily.PowerShell, ProbeTools = []
                });
                var snapshot = await context.RefreshAsync();
                Check(snapshot.Family == ShellFamily.PowerShell && !string.IsNullOrWhiteSpace(snapshot.ShellVersion), "Missing shell facts.");
                Check(Path.TrimEndingDirectorySeparator(snapshot.WorkingDirectory) ==
                      Path.TrimEndingDirectorySeparator(workspace.DirectoryPath), "Probe used a different workspace.");
            }),
            ("real shell captures stderr and a nonzero exit status", async () =>
            {
                await using var shell = CreateShell(Fresh());
                var result = await shell.RunAsync("throw 'synthetic command failure'");
                Check(result.ExitCode != 0 && result.Stderr.Contains("synthetic command failure"), "A failing shell call looked successful.");
            }),
            ("real shell reports timeout instead of successful completion", async () =>
            {
                await using var shell = CreateShell(Fresh(), TimeSpan.FromSeconds(1));
                var result = await shell.RunAsync("Start-Sleep -Seconds 3");
                Check(result.TimedOut && result.ExitCode == 124, "Missing executor timeout status.");
            }),
            ("command stays pending until the SDK receives approval", async () =>
            {
                var workspace = Fresh();
                await using var shell = CreateShell(workspace);
                using var client = new ScriptedChatClient([RenameCommand(workspace)]);
                var tool = shell.AsAIFunction("run_shell", "Synthetic test command.", requireApproval: true);
                Check(tool is ApprovalRequiredAIFunction, "Approval wrapper was lost.");
                var agent = CreateAgent(client, tool);
                var session = await agent.CreateSessionAsync();
                var response = await agent.RunAsync("Tidy the synthetic files.", session);
                var request = response.Messages.SelectMany(message => message.Contents).OfType<ToolApprovalRequestContent>().Single();
                Check(workspace.Files.All(file => File.Exists(Path.Combine(workspace.DirectoryPath, file.OriginalName))), "Command ran before approval.");
                await agent.RunAsync([new ChatMessage(ChatRole.User, [request.CreateResponse(false, "Denied by test.")])], session);
                Check(workspace.Files.All(file => File.Exists(Path.Combine(workspace.DirectoryPath, file.OriginalName))), "Denied command ran.");
            }),
            ("approved commands expose SDK results and host verification", async () =>
            {
                var workspace = Fresh();
                var text = await RunConsoleAsync(workspace,
                    ["Get-ChildItem -File | Select-Object -ExpandProperty Name", RenameCommand(workspace)],
                    "Tidy the synthetic files.\n y \nYES\n/exit\n");
                Check(text.Contains("PROPOSED COMMAND #1") && text.Contains("TOOL RESULT #1"), "Missing first command/result.");
                Check(text.Contains("TOOL RESULT #2") && text.Contains("exit_code: 0"), "Missing actual executor exit status.");
                Check(text.Contains("HOST VERIFIED: 4/4"), "No host-verified completion.");
                Check(DemoVerifier.Verify(workspace, TextWriter.Null), "Console claimed success without correct files.");
            }),
            ("denied commands never appear as executed results", async () =>
            {
                var workspace = Fresh();
                var text = await RunConsoleAsync(workspace, [RenameCommand(workspace)], "Tidy.\nn\n/exit\n");
                Check(text.Contains("DENIED - command not executed"), "Missing explicit denial.");
                Check(!text.Contains("HOST VERIFIED:") && !text.Contains("exit_code: 0"), "Denial was presented as execution.");
                Check(workspace.Files.All(file => File.Exists(Path.Combine(workspace.DirectoryPath, file.OriginalName))), "Denied rename changed files.");
            }),
            ("closed input stops without executing pending commands", async () =>
            {
                var workspace = Fresh();
                var text = await RunConsoleAsync(workspace, [RenameCommand(workspace)], "Tidy.\n");
                Check(text.Contains("Input closed."), "EOF did not stop the approval loop.");
                Check(workspace.Files.All(file => File.Exists(Path.Combine(workspace.DirectoryPath, file.OriginalName))), "EOF executed a pending command.");
            }),
            ("model completion claims cannot replace host verification", async () =>
            {
                var text = await RunConsoleAsync(Fresh(), [], "Say that you finished.\n/exit\n");
                Check(text.Contains("Model claims everything is finished."), "Missing synthetic model claim.");
                Check(text.Contains("HOST CHECK: NOT COMPLETE") && !text.Contains("HOST VERIFIED:"), "Model claim bypassed verification.");
            }),
            ("local verify and unsupported mode commands do not call the model", async () =>
            {
                var workspace = Fresh();
                await using var shell = CreateShell(workspace);
                using var client = new ScriptedChatClient([]);
                var agent = CreateAgent(client, shell.AsAIFunction("run_shell", "Synthetic test command.", requireApproval: true));
                using var output = new StringWriter();
                await AgentConsoleRunner.RunAsync(agent, workspace,
                    new StringReader("/verify\n/mode execute\n/exit\n"), output);
                Check(client.Calls == 0 && output.ToString().Contains("HOST CHECK: NOT COMPLETE"), "Local command invoked inference.");
                Check(output.ToString().Contains("mode switching is not part of this sample"), "Mode command was silently ignored.");
            })
        ];

        var failures = 0;
        try
        {
            foreach (var test in tests)
            {
                try
                {
                    await test.Run();
                    Console.WriteLine($"PASS: {test.Name}");
                }
                catch (Exception exception)
                {
                    failures++;
                    Console.Error.WriteLine($"FAIL: {test.Name}{Environment.NewLine}{exception}");
                }
            }
        }
        finally
        {
            // This root was allocated by this test run, never supplied by a caller.
            root.Delete(recursive: true);
        }
        Console.WriteLine($"{tests.Length - failures}/{tests.Length} Sample 21 checks passed (offline model, real MAF and PowerShell).");
        return failures == 0 ? 0 : 1;
    }

    private static LocalShellExecutor CreateShell(DemoWorkspace workspace, TimeSpan? timeout = null) =>
        new(new LocalShellExecutorOptions
        {
            Shell = "pwsh",
            WorkingDirectory = workspace.DirectoryPath,
            ConfineWorkingDirectory = true,
            Timeout = timeout ?? TimeSpan.FromSeconds(15),
            MaxOutputBytes = 4096
        });

    private static AIAgent CreateAgent(IChatClient client, AIFunction tool) =>
        client.AsHarnessAgent(new HarnessAgentOptions
        {
            DisableAgentSkillsProvider = true, DisableFileMemory = true,
            DisableTodoProvider = true, DisableWebSearch = true,
            DisableAgentModeProvider = true, DisableToolAutoApproval = true,
            ChatOptions = new ChatOptions { Tools = [tool], Instructions = DemoInstructions.Text }
        });

    private static async Task<string> RunConsoleAsync(DemoWorkspace workspace, string[] commands, string input)
    {
        await using var shell = CreateShell(workspace);
        using var client = new ScriptedChatClient(commands);
        var agent = CreateAgent(client, shell.AsAIFunction("run_shell", "Synthetic test command.", requireApproval: true));
        using var output = new StringWriter();
        await AgentConsoleRunner.RunAsync(agent, workspace, new StringReader(input), output);
        return output.ToString();
    }

    private static string RenameCommand(DemoWorkspace workspace) =>
        string.Join("; ", workspace.Files.Select(file =>
            $"Rename-Item -LiteralPath '{file.OriginalName}' -NewName '{file.ExpectedName}'"));

    private static void RenameFixture(DemoWorkspace workspace)
    {
        // Build a known filesystem state to test the verifier; production code never renames.
        foreach (var file in workspace.Files)
        {
            File.Move(Path.Combine(workspace.DirectoryPath, file.OriginalName),
                Path.Combine(workspace.DirectoryPath, file.ExpectedName));
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
