# Troubleshooting

| Symptom | Action |
|---|---|
| Missing configuration | Run the root setup helper with `-Session 4 -Check`; it shows key presence, not values |
| NuGet cannot resolve known packages | Use the repository `NuGet.Config`; it clears an inherited disabled-source setting locally, without editing global configuration |
| Hyperlight cannot initialize | Use `dotnet run`/the apphost on a supported machine; explicitly select `--no-codeact` when unavailable |
| Shell is unavailable | Install/locate PowerShell 7 or explicitly select `--no-shell`; do not silently choose another interpreter |
| Fixture script exhausted | The console fixture supports one portfolio query; restart it instead of treating the script as a general model |
| Evaluation returns exit 1 with `--inject-regression` | Expected: the deliberately wrong number must fail |
| HTTP 200 but the response failed | Inspect the Responses envelope's `status`; only `completed` is success |
| Duplicate storage error | Preserve the hosted factory's history ownership and `StoredOutputEnabled=false`; do not bypass the SDK guard |
| Memory path `.` is rejected | The provider uses the valid `facts` working folder beneath a fixed current-user store |
| Privacy-drop warning | An immutable event payload caused a span to be dropped deliberately; inspect safe counters, not the private payload |
| OTLP receives nothing | Check HTTP/protobuf, the collector base URI and distinct signal paths; verify the loopback transport test first |
| Purview/remote evaluation unavailable | Keep the explicit prerequisite result; do not report it as a live pass |

Provider failures are categorized without forwarding raw endpoints or credentials.
Use a private debugger for deeper diagnostics; do not paste raw Azure exceptions
into a public issue, log, slide or stream.
