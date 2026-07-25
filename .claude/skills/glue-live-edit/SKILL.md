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

The `"{DtoTypeName}:{json}"` string above is the *inner* payload. It's wrapped in a JSON `Packet {PacketType, Payload}` (PacketType `"OldDTO"`) and carried by the actual transport, `GameJsonCommunicationPlugin.Common.GameConnectionManager` (`Common\GameConnectionManager.cs`, Glue side) ↔ its embedded twin (`Embedded\GameConnectionManager.cs`, game side). `CommandSender.SendPacketInternal` builds the `Packet`; each direction is a length-prefixed (8-byte size, then ASCII body) blocking send. Connection handshake: game connects two sockets and sends one identifying byte each — `1` = glueToGame, `2` = gameToGlue; Glue's listener `Accept`s exactly two and routes by that byte.

## Connection lifecycle & self-heal (landmine)

Each side runs a 100ms `StatusCheck` that reconnects (game side) / re-listens (Glue side) whenever the connection is marked dead — but **the dead-marking is the bug surface**. Both sides track health with a single `_isConnected`/`IsConnected` flag that historically was cleared in **only one place: the `finally` of the game→Glue *receive* loop.** A failure on the *send* direction (`glueToGameSocket.Send` throwing `SocketException` 10054 "forcibly closed") did **not** clear the flag, so `StatusCheck` never re-listened and every subsequent send hit the same dead socket forever — the classic "live edit connected once, now every Play/Edit switch fails and never recovers." The trigger is usually the *other* process's reconnect: when the game's receive loop throws, its `StartConnecting` **disposes both sockets** before reconnecting, and that dispose is the 10054 the still-"connected" Glue side sees.

Fix pattern (already applied in both `GameConnectionManager.cs` files): any send-path socket failure calls a `ResetConnection(reason)` that disposes **both** sockets and clears the flag, letting the existing `StatusCheck` re-handshake. Must be symmetric — reset both directions together, because the handshake is order-dependent (byte 1 then byte 2, two accepts); a half-reset desyncs. Diagnostics: the Glue side logs connect/disconnect/reset transitions via `PluginManager.CallPluginMethod("Compiler Plugin", "HandleOutput", ...)` (game side only reaches `Debug.WriteLine`, since its output can't cross the broken socket).

## Embedded → Generated: how "the editor injects code" works

`FRBDK\Glue\GameCommunicationPlugin\GlueControl\Embedded\**\*.cs` are the **master source templates** for the entire runtime live-edit system (command receiver, DTOs, editing manager, variable assignment, models, etc.) — hand-edit these, never the copies.

`EmbeddedCodeManager.EmbedAll()` (`CodeGeneration\EmbeddedCodeManager.cs`) copies each listed file into the game project under `GlueControl/`, converting `Editing.Managers.GlueCommands.cs` → `Editing/Managers/GlueCommands.Generated.cs` (dots become path separators, `.Generated` suffix added). This is what runs on Glux load / whenever live-edit settings change (`HandleGluxLoaded`, `HandlePortOrGenerateCheckedChanged` in `MainCompilerPlugin.cs`). The game project therefore contains a full mirrored copy of the DTOs and runtime logic — there is no shared assembly between Glue and the game.

## Gotchas

- **The debug/edit-mode hook is `CustomActivityEditMode()`, not "CustomDebugActivity."** `ScreenManager` calls `Screen.ActivityEditMode()` (virtual, `Engines\FlatRedBallXNA\FlatRedBall\Screens\Screen.cs`) instead of normal `Activity` while `ScreenManager.IsInEditMode` is true. Per-element codegen (`CodeWriter.GenerateActivityEditMode`, `FRBDK\Glue\Glue\CodeGeneration\CodeWriter.cs:1422`) calls each named object's own `ActivityEditMode()` and then `CustomActivityEditMode()` — an empty `partial void` users can implement in their hand-written partial class, following the normal generated/custom partial-class split.
- **Variable edits during live play are an overlay, not real codegen.** Since the game can't reload generated code while running, edited values are applied through `GlueControl.Editing.VariableAssignmentLogic.SetVariable` (`Embedded\Editing\VariableAssignmentLogic.cs`) — a large, manually-maintained switch over variable name/type/target-instance-kind (collision relationships, tile shape collections, states, lists, `AttachToContainer`, etc.) that reflects/`screen.ApplyVariable`s the value onto the live instance. Any variable kind not special-cased here either falls through to generic reflection (works for simple properties) or silently fails to apply — this is the brittleness the user should expect: a new variable type showing up correctly in Glue but not visually updating live almost always means this file needs a new case, not that the DTO plumbing is broken. Actual codegen only happens on the next full rebuild.
- **`RefreshManager.ShouldRestartOnChange` / `CreateStopAndRestartTask` is the "give up and restart" escape valve.** Many Glue-side changes (new variable on an existing type, excluding a variable from a state category, failed object-add/remove round trips) aren't attempted live at all — they just queue a stop+rebuild+relaunch. If a live-edit feature "doesn't work," check whether the relevant `RefreshManager`/`VariableSendingManager` handler actually attempts a live push or just restarts.
- File-change filtering: `RefreshManager.GetIfShouldReactToFileChange` explicitly ignores `*.Generated.cs`/`*.Generated.xml` changes so that codegen's own file writes don't trigger a feedback loop of restarts.
- **`ReactToPlayOrEditSet()` (`MainCompilerPlugin.cs`) fires twice per launch-into-edit-mode — once too early.** `GameHostController.StartRunInEditMode` sets `IsEditChecked = true` before `Compile()`/`DoRun` run (so the toolbar shows edit mode while building), which fires `PlayOrEdit`'s change handler and calls `ReactToPlayOrEditSet()` while the game process doesn't exist yet — guarded with an `IsRunning` early-out now, since the real send happens later via `Runner_GameStarted`. The command-line `IsInEditMode=` launch arg (`Game1GlueControlGenerator.cs`) looks like an alternate path but its handling is commented out/dead — the socket DTO is the only mechanism.

## Camera: edit mode vs. game mode

`Embedded\Editing\CameraLogic.cs` (static class `GlueControl.Editing.CameraLogic`) is the edit-mode camera controller. It manipulates the same `Camera.Main` singleton directly (no separate edit-camera object) and saves/restores position+zoom per screen type in a dictionary, so each screen remembers its last edit-mode camera state. Zoom is a discrete lookup table (`zoomLevels[]`, 10000%→5%) driven by mouse wheel / Ctrl+/-; panning is middle-mouse drag or edge-of-window drag-scroll.

Game mode's camera is set up by generated `Setup/CameraSetup.Generated.cs` (from `FRBDK\Glue\Glue\Plugins\EmbeddedPlugins\CameraPlugin\CameraSetupCodeGenerator.cs`), not by `CameraLogic.cs` at all — that class only compiles into live-edit builds.

| Behavior | Game mode | Edit mode |
|---|---|---|
| Zoom | Fixed at startup (`ResetCamera`/`SetupCamera`); only changes via window resize with `IncreaseVisibleArea`, or an opt-in `CameraControllingEntity.ApplyZoom()` | Freely adjustable — mouse wheel / hotkeys via `CameraLogic.UpdateCameraToZoomLevel()` |
| Bounds | Unclamped by default; clamping is opt-in per-screen via `CameraControllingEntity` (needs a `Map` assigned) — `Engines\FlatRedBallXNA\FlatRedBall\Entities\CameraControllingEntity.cs:309-393` | Always unclamped — no bounds code exists anywhere under `GlueControl\Embedded` |
| Aspect ratio | Fixed per `DisplaySettings.AspectRatioWidth/Height`; pillarbox/letterbox via `CameraSetup.SetAspectRatioTo` computing a `DestinationRectangle` smaller than the backbuffer | Unconstrained — Glue sends `SetCameraAspectRatioDto` with `AspectRatio = null` on entering edit mode (`MainCompilerPlugin.cs:851-876`), which makes `SetAspectRatioTo` fill the whole window with no bars |

The edit/game aspect-ratio toggle is a **one-shot DTO on Play/Edit switch**, not a persistent `if IsInEditMode` check in the render loop — `CommandReceiver.HandleDto(SetCameraAspectRatioDto)` (`Embedded\CommandReceiver.cs:850-863`) calls `CameraSetup.ResetCamera()` once and, if already in edit mode, also `CameraLogic.UpdateCameraToZoomLevel()`.

### Gum zoom — high-landmine area, read before touching zoom code

Gum renders UI in its own **pixel-space canvas** (`GraphicalUiElement.CanvasWidth/Height`), completely separate from FRB's world-space `Camera.Main`. This means **two independent zoom values must be kept in sync by hand** on every camera zoom change:

1. `Camera.Main.OrthogonalHeight` — the FRB world camera.
2. `RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom` + per-layer `LayerCameraSettings.Zoom` — Gum's own scale factor.

`CameraLogic.UpdateCameraToZoomLevel()` (`Embedded\Editing\CameraLogic.cs:293-359`) sets both, then **must** call `CameraSetup.ResetGumResolutionValues()` (generated by `CameraSetupCodeGenerator.cs:217-310`) to reset `CanvasWidth/Height` — forgetting that call after any future zoom-code change is the single most likely regression, since it's not obviously wired to `OrthogonalHeight`.

Known landmines, in order of how likely they are to bite:

- **Window resize races the zoom sync.** `GraphicsOptions.SizeOrOrientationChanged` (fires on device reset, `FlatRedBallServices.cs:322,347`) is wired to generated `HandleResolutionChange` (`CameraSetupCodeGenerator.cs:688-706`), which *also* calls `ResetGumResolutionValues()` — but recomputes canvas/zoom from base `Data.ResolutionWidth/Height`, not from `CameraLogic`'s current edit-mode zoom level. Resizing the window while zoomed in edit mode can silently overwrite Gum's zoom back toward the base-resolution value.
- **Per-layer desync.** `LayerCameraSettings.Zoom` is set in a `foreach (layer in Renderer.Layers)` loop with a null-check on `LayerCameraSettings`. Any Gum layer added *after* that loop runs, or whose `LayerCameraSettings` is null at creation (`GumLayerAssociationCodeGenerator.cs:126`, `Renderer.AddLayer`), silently keeps stale/default zoom — a plausible cause of "this one Gum layer looks wrong when zoomed" bugs.
- **Dead-code false trail.** `PositionedObjectGueWrapper.cs:118-123` (Gum-object-attached-to-FRB-object positioning) has an old naive formula (`zoom = DestinationRectangle.Height / OrthogonalHeight`) left commented out — it was wrong and replaced in commit `0dd8b9d3b` (2022-03-18) with a ratio-based conversion (screen-space fraction × `CanvasWidth/Height`, lines ~134-140). Do not resurrect the commented formula when debugging positioning-while-zoomed issues.
- The shipped/generated `CameraLogic.Generated.cs` in real projects has historically matched the `Embedded` template byte-for-byte (verified against a real project) — if you find a project where it doesn't, that diff itself is signal of a manual workaround worth investigating.

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
