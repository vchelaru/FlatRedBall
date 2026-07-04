---
name: glue-live-edit
description: Glue↔running-game live edit (drag/resize/tweak a live game, changes flow back to Glue). Triggers: GlueControl, GameConnectionManager, CommandReceiver, VariableAssignmentLogic, EmbeddedCodeManager, ActivityEditMode, edit mode.
---

# Glue Live Edit

Lets a user run a game launched by Glue, switch it into edit mode, and move/resize/tweak objects live. Edits push back to Glue for persistence + codegen. This is FRB1-only — not being redesigned for FRB2, so treat its foundation as fixed rather than something to refactor.

## Architecture

Two processes talk over **two raw TCP sockets on loopback** (`GameConnectionManager`, one per direction), port from `CompilerSettings.json` (random 8000-8999 per project). Messages are plain text: `"{DtoTypeName}:{json payload}"`.

- **Glue → game**: `CommandSender.Self.Send(dto)` (`FRBDK\Glue\GameCommunicationPlugin\GlueControl\CommandSending\CommandSender.cs`) serializes a DTO and sends it. Game's `CommandReceiver.Receive` splits on the first `:`, reflects over its own `HandleDto` overloads to find one whose single parameter type name matches, deserializes, and dispatches.
- **Game → Glue** (e.g. drag/resize results): the game enqueues onto `GlueControlManager.GameToGlueCommands` (a `ConcurrentQueue`); `GlueControlManager`'s socket loop drains it and writes to the `gameToGlueSocket`.
- `RefreshManager` (Glue side) is the hub that decides, for every kind of Glue-side change (new object, renamed element, variable edit, state created, file changed...), whether to push an incremental command or fall back to `CreateStopAndRestartTask` (kill + rebuild + relaunch the game) when a live update isn't supported.
- `VariableSendingManager` turns a Glue property-grid change into `GlueVariableSetData` DTOs (handles `X/Y/Z` → `RelativeX/Y/Z` when attached, collision relationships, tile shape collections, states, etc.) before `CommandSender` ships them.

## Embedded → Generated: how "the editor injects code" works

`FRBDK\Glue\GameCommunicationPlugin\GlueControl\Embedded\**\*.cs` are the **master source templates** for the entire runtime live-edit system (command receiver, DTOs, editing manager, variable assignment, models, etc.) — hand-edit these, never the copies.

`EmbeddedCodeManager.EmbedAll()` (`CodeGeneration\EmbeddedCodeManager.cs`) copies each listed file into the game project under `GlueControl/`, converting `Editing.Managers.GlueCommands.cs` → `Editing/Managers/GlueCommands.Generated.cs` (dots become path separators, `.Generated` suffix added). This is what runs on Glux load / whenever live-edit settings change (`HandleGluxLoaded`, `HandlePortOrGenerateCheckedChanged` in `MainCompilerPlugin.cs`). The game project therefore contains a full mirrored copy of the DTOs and runtime logic — there is no shared assembly between Glue and the game.

## Gotchas

- **The debug/edit-mode hook is `CustomActivityEditMode()`, not "CustomDebugActivity."** `ScreenManager` calls `Screen.ActivityEditMode()` (virtual, `Engines\FlatRedBallXNA\FlatRedBall\Screens\Screen.cs`) instead of normal `Activity` while `ScreenManager.IsInEditMode` is true. Per-element codegen (`CodeWriter.GenerateActivityEditMode`, `FRBDK\Glue\Glue\CodeGeneration\CodeWriter.cs:1422`) calls each named object's own `ActivityEditMode()` and then `CustomActivityEditMode()` — an empty `partial void` users can implement in their hand-written partial class, following the normal generated/custom partial-class split.
- **Variable edits during live play are an overlay, not real codegen.** Since the game can't reload generated code while running, edited values are applied through `GlueControl.Editing.VariableAssignmentLogic.SetVariable` (`Embedded\Editing\VariableAssignmentLogic.cs`) — a large, manually-maintained switch over variable name/type/target-instance-kind (collision relationships, tile shape collections, states, lists, `AttachToContainer`, etc.) that reflects/`screen.ApplyVariable`s the value onto the live instance. Any variable kind not special-cased here either falls through to generic reflection (works for simple properties) or silently fails to apply — this is the brittleness the user should expect: a new variable type showing up correctly in Glue but not visually updating live almost always means this file needs a new case, not that the DTO plumbing is broken. Actual codegen only happens on the next full rebuild.
- **`RefreshManager.ShouldRestartOnChange` / `CreateStopAndRestartTask` is the "give up and restart" escape valve.** Many Glue-side changes (new variable on an existing type, excluding a variable from a state category, failed object-add/remove round trips) aren't attempted live at all — they just queue a stop+rebuild+relaunch. If a live-edit feature "doesn't work," check whether the relevant `RefreshManager`/`VariableSendingManager` handler actually attempts a live push or just restarts.
- File-change filtering: `RefreshManager.GetIfShouldReactToFileChange` explicitly ignores `*.Generated.cs`/`*.Generated.xml` changes so that codegen's own file writes don't trigger a feedback loop of restarts.

## Key files

| Side | File | Purpose |
|---|---|---|
| Glue | `GameCommunicationPlugin\GlueControl\MainCompilerPlugin.cs` | Plugin entry point; owns build/run/edit-mode toggle, wires up embedding + codegen on Glux load |
| Glue | `Managers\GameHostController.cs` | Launches the game process, embeds its window in the Game tab, builds run args (`IsInEditMode=true`, startup screen) |
| Glue | `Managers\RefreshManager.cs` | Central dispatcher: Glue-side change → live command vs. stop/rebuild/restart |
| Glue | `Managers\VariableSendingManager.cs` | Glue property-grid change → `GlueVariableSetData` DTO(s) |
| Glue | `CommandSending\CommandSender.cs` | Serializes + sends DTOs, wraps `GameConnectionManager` socket calls |
| Glue | `Dtos\Dtos.cs` | Glue-side DTO definitions (mirrored, not shared, with the game copy) |
| Glue | `CodeGeneration\EmbeddedCodeManager.cs` | Copies `Embedded\*.cs` → game's `GlueControl\*.Generated.cs` |
| Glue | `Embedded\**` | Master source for everything the game gets — edit these, not the `.Generated.cs` copies in a game project |
| Game (generated) | `GlueControl\GlueControlManager.Generated.cs` | Runtime entry point; owns the socket, the `GameToGlueCommands` queue, edit-mode state |
| Game (generated) | `GlueControl\CommandReceiver.Generated.cs` | Deserializes incoming DTOs by type name, dispatches to `HandleDto` overloads |
| Game (generated) | `GlueControl\Editing\EditingManager.Generated.cs` | Selection, drag/resize input handling, pushes changes into `GameToGlueCommands` |
| Game (generated) | `GlueControl\Editing\VariableAssignmentLogic.Generated.cs` | The brittle live variable-overlay logic (see Gotchas) |
| Game (generated) | `GlueControl\Screens\EntityViewingScreen.Generated.cs` | Sandbox screen used when live-editing a single Entity outside any Screen |
