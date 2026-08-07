---
name: glue-live-game-testing
description: Testing GlueControl's embedded runtime (CommandReceiver, GlueControlManager) against a real running game process. Triggers: LiveGameProcess, live edit runtime behavior, ScreenManager.Start vs MoveToScreen, testing Embedded/*.cs, Category=LiveGame.
version: 1.0.0
---

# Glue Live Game Testing

`GameCommunicationPlugin/GlueControl/Embedded/*.cs` (CommandReceiver, GlueControlManager, EditingManager,
...) is `<Compile Remove>`d from `GameCommunicationPlugin.csproj` — it only exists inside a compiled,
running game process, never inside Glue itself. [glue-unit-test-bootstrap](../glue-unit-test-bootstrap/SKILL.md)'s
`GoldProjectCompileTests` prove that closure *compiles*; they never run it. If a bug is in what
`CommandReceiver.HandleDto` actually *does* at runtime (screen transitions, entity selection, edit-mode
state), a compile-only test cannot catch it — you need a real running game process.

## The harness

`GlueUnitTests/TestSupport/LiveGameProcess.cs` builds a gold project, launches its real built `.exe`, and
drives it through Glue's actual `CommandSender`/`GameJsonCommunicationPlugin.Common.GameConnectionManager`
socket protocol — the same wire protocol production Glue uses, not a stand-in.

```csharp
[Trait("Category", "LiveGame")]
[StaFact]
public async Task MyTest()
{
    GlueTestBootstrap.EnsureGameProjectPluginsRegistered();
    using var game = await LiveGameProcess.StartAsync(
        "Samples/EditorTest1",
        csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
        exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe");

    var screenName = await game.GetCurrentScreenName();   // "" if no screen loaded
    var response = await game.SelectEntity("Entities\\Entity1");
}
```

`StartAsync` (default `refreshLiveEditCodeFromSource: true`) loads the copied project into Glue and calls
`GoldProject.EmbedLiveEditCode()` before building, so the test exercises the CURRENT branch's
`Embedded/*.cs`, not whatever was checked in. See `LiveGameProcessTests.cs` for the two existing tests.

## Adding a new drive/observe method

Follow `GetCurrentScreenName()`/`SelectEntity()`: build the real DTO
(`GameCommunicationPlugin.GlueControl.Dtos`) and call `CommandSender.Self.Send(dto)` directly.

**Landmine — do not set `GlueState.Self.CurrentEntitySave`/`CurrentScreenSave`/`CurrentNamedObjectSave` to
drive selection.** Their setters route through `GlueState.Find.TreeNodeByTag(value)`, which needs a real,
populated WPF tree view. There isn't one in this headless host, so the assignment silently no-ops
(`CurrentElement` stays null) and `RefreshManager.PushGlueSelectionToGame` sends nothing — no exception, no
signal, just a test that mysteriously never sees the effect. Skip Glue's UI-bound selection state entirely
and build the DTO by hand.

## Only `Samples/EditorTest1` works as a target project today

`LiveGameProcess` needs a project whose `Game1.Generated.cs` already constructs
`GlueControlManager`/`GameConnectionManager` — i.e. one that went through a real "enable live edit" Glue
session. That wiring comes from `MainCompilerPlugin.HandleGluxLoaded` (`Game1GlueControlGenerator`), and
`MainCompilerPlugin` cannot run in the test host (it builds real WPF tabs and opens sockets on
registration — same reason `GoldProject.EmbedLiveEditCode()` calls `EmbeddedCodeManager.EmbedAll` directly
instead of going through it). So `Game1.Generated.cs` can't be regenerated headlessly; `LiveGameProcess`
preserves it exactly as checked in (only patching its port) rather than deleting/regenerating it.

`Samples/EditorTest1` is checked in with its `Generated.cs` committed - a `.gitignore` exception like
`BeefballKni`'s, since `*.Generated.cs` is gitignored repo-wide otherwise. To add another target project:
turn on live edit for real in a real Glue session, copy the project in the same way (exclude `bin`/`obj`,
keep the sibling `.sln` - `ProjectLoader` needs it), add the same two-line `.gitignore` exception.

## Wire protocol, if you need to touch it

Game connects OUT to Glue (Glue listens). Two separate TCP sockets, one per direction, each opened with a
1-byte handshake (`1` = glue→game, `2` = game→glue) — see `GameCommunicationPlugin/Common/GameConnectionManager.cs`
(Glue-side, server) and `GlueControl/Embedded/GameConnectionManager.cs` (game-side, client, namespace
`GlueCommunication`). Port is baked into `Game1.Generated.cs` as a literal int at two call sites (was
`8846` in the checked-in fixture) - `LiveGameProcess` text-patches both before building, to a fresh port
per run so it never collides with a real Glue instance on the dev machine.

The actual DTO dispatch is `GlueControlManager.ProcessMessage` (`Embedded/GlueControlManager.cs`) -
`"GetCurrentScreen"` is a raw-string command handled specially; everything else is `"{DtoTypeName}:{json}"`
routed to `CommandReceiver.Receive`/`HandleDto(SelectObjectDto)` etc.

## CI

Tagged `Category=LiveGame`, excluded from both `Category!=BuildSmoke` and `Category=BuildSmoke` CI filters
(see `pr-tests.yml`/`glue.yml`) - it opens a real MonoGame DesktopGL window, and GitHub-hosted Windows
runners aren't guaranteed a display/GPU context. Developer-machine-only:
`dotnet test ... --filter "Category=LiveGame"`.
