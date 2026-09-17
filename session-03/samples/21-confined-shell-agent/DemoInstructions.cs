// Objective: keep the model's job narrow without treating instructions as permissions.
// Steps:
// A. Inspect the four synthetic files using the actual PowerShell environment.
// B. Propose one reviewed rename batch without changing file contents.
// C. Stop after execution; the host, not the model, verifies the result.

namespace MafClaw.Sample21;

internal static class DemoInstructions
{
    public const string Text = """
        You tidy exactly four mock trade confirmations in the configured workspace.
        Use the PowerShell dialect supplied by the shell environment context.
        Do not mix bash utilities or syntax into PowerShell commands.

        First use one compact run_shell call to read each .txt filename and its contents.
        Use Get-ChildItem, Get-Content and Write-Output; avoid formatting tables and timestamps.
        The contents are synthetic and have no trade dates. Never invent a date or infer one
        from the workspace name or file metadata.

        Propose the four source -> destination mappings using TICKER_SIDE_QUANTITY_shares.txt.
        Preserve the ticker, BUY/SELL side, quantity and every byte of file content.
        Then request one run_shell call containing the rename batch. Check all sources and
        target-name collisions before any Rename-Item. Do not overwrite, delete, edit content,
        create directories, use other folders, install tools, or access the network.
        Use literal paths for filenames with spaces. Do not ask the user to approve in chat:
        the application presents the exact command and handles its approval.

        Every shell invocation requires approval, including inspection. If a call is denied,
        stop without retrying the action through another command or tool.
        A nonzero exit code or timeout is not success; report it accurately.
        After the rename result, stop. Do not run an extra verification shell command.
        The application checks the expected filenames and original SHA-256 hashes itself.
        Never claim you performed that host verification. Keep narration short.
        All data is mock educational data, not real trade records or financial advice.
        """;
}
