---
name: glue-embedded-game-preview
description: How the Game tab's live preview embeds and resizes the real running game window, and its zoom/resolution status bar. Triggers: GameHostView, WinformsHost, Runner_MoveWindow, BottomStatusBar, ZoomControl, CurrentDisplayInfoDto, SetParent game window.
version: 1.0.0
---

# Glue Embedded Game Preview

The "Game" tab does not render the game into a WPF surface. Glue launches the game as a real,
separate process and reparents its native window (`SetParent`, `GameHostView.xaml.cs::EmbedHwnd`)
into a WinForms `Panel` hosted by a `WindowsFormsHost` (`GameHostView.xaml` → `WinformsHost`).

## Resize flow

`WinformsHost_SizeChanged`/`MainGrid_SizeChanged` → `SetGameToEmbeddedGameWindow` sends a
`Runner_MoveWindow` command — not over the DTO socket, but as a raw `_eventCaller`/`ReactToPluginEvent`
string handled by `CompilerPlugin.HandleEvent` (`case "Runner_MoveWindow"`), which calls
`Runner.MoveWindow` → the real Win32 `MoveWindow` API directly on `gameHandle`. Every resize is the
*real game process's* actual OS window changing size/position for real, in-process — there's no visual
scaling on Glue's side, and whatever the game's own `AspectRatioBehavior`/`ResizeBehavior` (see below)
does with that size happens the same as it would for an end user.

`WinformsHost` and its underlying WinForms `Panel` (`winformsPanel`, `GameHostView` constructor) always
stay full-size, filling `MainGrid` (Stretch, no explicit size/alignment) — **even in fixed-size preview
mode.** Letterboxing is achieved by moving/sizing only the real embedded game window (via
`Runner_MoveWindow`'s X/Y/Width/Height) smaller than `winformsPanel`, whose own dark `BackColor`
(`FromArgb(30,30,30)`, set in the `GameHostView` constructor) shows through the margins as letterbox
bars.

**Landmine — do not center by resizing/repositioning `WinformsHost`/`winformsPanel` (the ANCESTOR)
instead of the game window itself.** An earlier version of fixed-size preview did exactly that
(`WinformsHost.HorizontalAlignment/VerticalAlignment = Center` + explicit `Width`/`Height`, always
passing `X=0,Y=0` to `Runner_MoveWindow`). It looked correct visually but broke rectangle-select and
zoom-around-cursor by exactly the centering offset: `gameHandle`'s position *relative to its own
immediate parent* (`winformsPanel`) never changed, so it never received a native move notification —
only its *ancestor* moved. MonoGame/SDL's cached window position (used to translate the cursor's
absolute screen position into client-relative mouse coordinates) went stale by that offset. The fix
was to always keep `winformsPanel` full-size/unmoved and instead pass the letterbox offset as
`Runner_MoveWindow`'s `X`/`Y` — since that's a real position change to `gameHandle` relative to its own
immediate parent, it generates the native move notification MonoGame/SDL needs.

## Zoom / resolution status bar

`BottomStatusBar.xaml` (`ZoomControl` + resolution `TextBlock`) is fed by
`CurrentDisplayInfoDto`, pushed from `GlueControl.Editing.CameraLogic` (`Embedded/Editing/CameraLogic.cs`,
game process only — `PushZoomLevelToEditor`) and applied on the Glue side in
`CommandReceiving/CommandReceiver.cs::HandleDto(CurrentDisplayInfoDto)`, which writes
`CompilerViewModel.CurrentZoomLevelDisplay`/`ResolutionDisplayText`. Zoom itself (`ChangeZoomDto`,
`+`/`-`) is a separate concept from window size — it scales `Camera.Main.OrthogonalHeight` inside the
game and is independent of what size the embedded OS window actually is. None of this zoom state is
persisted to the project file; it resets every Glue launch.

## Landmine — `GameCommunicationPlugin.csproj` has no implicit globbing

`<EnableDefaultCompileItems>false</EnableDefaultCompileItems>` means a new `.cs` file under this
project (e.g. adding a class next to `GameHostView.xaml.cs`) silently compiles into nothing until
it's added to the `<Compile Include="GlueControl\Views\...` `<ItemGroup>` by hand - no error, the
type just doesn't exist for anything that references the assembly (`CS0103` in a consumer, not in
this project).

## Project's target resolution vs. live preview size

The project's configured target resolution/aspect ratio lives in
`DisplaySettingsViewModel`/`GlueCommon/SaveClasses/DisplaySettings.cs`
(`Glue/Plugins/EmbeddedPlugins/CameraPlugin/`) — edited via the Camera Settings panel
(`CameraSettingsControl.xaml`) — and normally only takes effect through codegen
(`CameraSetupCodeGenerator.cs`) for what a *built* game does at startup/on resize. By default the live
preview's `WinformsHost` panel ignores it and always stretches to fill the tab.

The "fixed-size preview" toggle (`BottomStatusBar`'s aspect-ratio icon, `CompilerViewModel.IsFixedSizePreview`,
issue #2035) is the one exception: when on, `GameHostView.SetGameToEmbeddedGameWindow` sizes/centers
the real embedded game window (not `WinformsHost` — see the resize-flow landmine above) to
`DisplaySettings.ResolutionWidth/Height` scaled to fit the panel
(`FixedSizePreviewCalculator`, unit-tested). **Landmine:** "100%" there is not the raw resolution —
it's `ResolutionWidth/Height * DisplaySettings.Scale / 100` (`FixedSizePreviewCalculator.GetEffectiveTargetResolution`),
since `Scale` (Camera Settings' desktop scale, e.g. 400%) is how a real launched window is already
upscaled from the project's internal resolution. Using the raw resolution previews the wrong size for
any project with a non-100% `Scale`.

## Landmine — editor zoom "100%" is not the same "100%" as a real launch

`CameraLogic`'s own zoom (`+`/`-`, `zoomLevels`) is an *editor-only* pan/zoom convenience: its 100%
means "1 world unit = 1 window pixel," computed purely from `Camera.Main.DestinationRectangle.Height`
(`UpdateCameraToZoomLevel`) — it has no idea what `Data.Scale` is. The real generated game
(`CameraSetupCodeGenerator.cs` ~line 696) instead sets
`Camera.Main.OrthogonalHeight = DestinationRectangle.Height / (Data.Scale / 100)`, which — at the
window's natural size (`ResolutionHeight * Scale / 100` pixels tall) — always works out to
`OrthogonalHeight == ResolutionHeight`, independent of `Scale`. `Scale` is a pure pixel-multiplier
(crisp upscaling), not a "show more/less world" zoom.

Fixed-size preview mode locks onto *that* value, not editor-zoom-100%: `CameraLogic.SetFixedSizePreviewLock`
pins `OrthogonalHeight` to a constant (`DisplaySettings.ResolutionHeight`, sent via
`SetFixedSizePreviewLockDto`) regardless of the embedded window's current pixel size — so when the Glue
panel is too small and the window gets letterbox-shrunk (`FixedSizePreviewCalculator`), the whole
picture optically shrinks uniformly (screenshot-thumbnail style) instead of revealing more world, which
is what using editor-zoom-100% would have done. `DoZoomPlus`/`DoZoomMinus` (mouse wheel, `Ctrl+/-`
hotkeys, and the Glue UI's `ChangeZoomDto` all route through these two) no-op while locked. The lock is
re-sent whenever `ResolutionChanged` fires (`MainCompilerPlugin`) but *not* `ScaleChanged` — Scale only
affects window pixel size (`SetGameToEmbeddedGameWindow`), never the locked `OrthogonalHeight` target.

## Landmine — editor zoom % *ratio* does match `Data.Scale`; don't assign `OrthogonalHeight` an absolute value

`DestinationRectangle.Height / OrthogonalHeight` is panel-size-invariant and equals `Data.Scale/100` at
the matching zoom level — only an absolute `OrthogonalHeight` assignment (vs. the divisor formula) breaks
that. See `UpdateCameraToZoomLevel`'s `forceToGameDefault` (issue #2044).
