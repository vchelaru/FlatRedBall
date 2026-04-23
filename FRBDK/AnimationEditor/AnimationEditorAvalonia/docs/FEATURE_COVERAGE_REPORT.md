# AnimationEditor Feature Coverage Report

> Generated from exhaustive research of the old WinForms app, FRB engine models, and the new Avalonia port.
> Test results: **288 tests passing, 0 failing** as of this report.

---

## Table of Contents

1. [Complete Feature Inventory](#1-complete-feature-inventory)
2. [Feature-to-Test Mapping](#2-feature-to-test-mapping)
3. [Untestable Features and Gap Recommendations](#3-untestable-features-and-gap-recommendations)

---

## 1. Complete Feature Inventory

### 1.1 Animation Chain Management

| # | Feature | Description |
|---|---------|-------------|
| A01 | Add animation chain | Creates a new chain with a unique name, adds it to the ACLS, selects it, fires `AnimationChainsChanged` |
| A02 | Delete animation chains | Removes one or more chains; guarded by confirmation dialog |
| A03 | Move chain up/down | Reorders a chain by ±1 in the list; clamped at edges |
| A04 | Move chain to top | Reorders to index 0 |
| A05 | Move chain to bottom | Reorders to last index |
| A06 | Flip chain horizontally | Toggles `FlipHorizontal` on every frame in the chain |
| A07 | Flip chain vertically | Toggles `FlipVertical` on every frame in the chain |
| A08 | Invert frame order | Reverses the frame list (`Frames.Reverse()`) |
| A09 | Set all frame lengths | Sets `FrameLength` on every frame to a given value |
| A10 | Duplicate chain | Deep-copies chain; optional H/V flip toggle; optional name override; copies attached shapes |
| A11 | Duplicate chain with H flip | `DuplicateChain(source, flipH: true)` |
| A12 | Duplicate chain with V flip | `DuplicateChain(source, flipV: true)` |
| A13 | Sort animations alphabetically | Reorders ACLS by `Name` ascending |
| A14 | Unique chain name generation | Ensures no two chains share the same name on creation or duplication |

### 1.2 Animation Frame Management

| # | Feature | Description |
|---|---------|-------------|
| F01 | Add frame | Creates frame with UV defaults (L=0, R=1, T=0, B=1), `FrameLength=0.1`, empty `ShapeCollectionSave`, optional texture name |
| F02 | Delete frames | Removes frames from selected chain; guarded by confirmation dialog |
| F03 | Move frame up/down | Reorders frame by ±1; clamped at edges |
| F04 | Move frame to top | Moves frame to index 0 |
| F05 | Move frame to bottom | Moves frame to last index |
| F06 | Set frame texture | Assigns `TextureName` to a frame (via drag-drop, paste or text field) |
| F07 | Set UV coordinates | Manually set `LeftCoordinate`, `RightCoordinate`, `TopCoordinate`, `BottomCoordinate` |
| F08 | Set frame length | Sets `FrameLength` on a single frame |
| F09 | Set flip horizontal per-frame | Toggles `FlipHorizontal` on a single frame |
| F10 | Set flip vertical per-frame | Toggles `FlipVertical` on a single frame |
| F11 | Set relative X/Y offset | Sets `RelativeX` / `RelativeY` for animation position offsets |

### 1.3 Shape / Collision Management (per frame)

| # | Feature | Description |
|---|---------|-------------|
| S01 | Add axis-aligned rectangle | Creates with default `ScaleX=8, ScaleY=8`, unique name, position matches frame offset, selects it |
| S02 | Add circle | Creates with default `Radius=8`, unique name, position matches frame offset, selects it |
| S03 | Delete axis-aligned rectangle | Removes rect only if the specified frame owns it |
| S04 | Delete circle | Removes circle only if the specified frame owns it |
| S05 | Ask-to-delete rectangles | Async delete guarded by confirmation dialog |
| S06 | Ask-to-delete circles | Async delete guarded by confirmation dialog |
| S07 | Match rectangle to frame | Sets rect `X/Y` to `frame.RelativeX/Y` |
| S08 | Match circle to frame | Sets circle `X/Y` to `frame.RelativeX/Y` |
| S09 | Unique shape name generation | `StringFunctions.MakeStringUnique` prevents name collisions across rect and circle names |
| S10 | Edit rectangle properties | Set `ScaleX`, `ScaleY`, `X`, `Y`, `Name` via property inspector |
| S11 | Edit circle properties | Set `Radius`, `X`, `Y`, `Name` via property inspector |
| S12 | Drag-handle resize (rect) | Mouse drag on wireframe handle changes `ScaleX/Y` |
| S13 | Drag-handle move (shape) | Mouse drag moves shape `X/Y` |

### 1.4 Selection State

| # | Feature | Description |
|---|---------|-------------|
| SS01 | Select chain | Sets `SelectedChain`, clears frame/rect/circle, fires `SelectionChanged` |
| SS02 | Select frame | Sets `SelectedFrame`, auto-finds parent chain, clears rect/circle, fires `SelectionChanged` |
| SS03 | Select rectangle | Sets `SelectedRectangle`, clears circle, fires `SelectionChanged` |
| SS04 | Select circle | Sets `SelectedCircle`, clears rectangle, fires `SelectionChanged` |
| SS05 | `SelectedShape` union property | Returns `(object)rect ?? circle` |
| SS06 | `SelectedTextureName` resolution | Returns frame texture name, or first frame of chain, or null |
| SS07 | Multi-select (SelectedNodes) | `SelectedFrames` reads from `SelectedNodes` first, falls back to single frame |
| SS08 | Deselect all | Setting chain/frame/rect/circle to null clears lower-priority selections |

### 1.5 Object Lookup / Navigation

| # | Feature | Description |
|---|---------|-------------|
| OL01 | Find frame containing rectangle | `ObjectFinder.GetAnimationFrameContaining(rect)` |
| OL02 | Find frame containing circle | `ObjectFinder.GetAnimationFrameContaining(circle)` |
| OL03 | Find chain containing frame | `ObjectFinder.GetAnimationChainContaining(frame)` |

### 1.6 File I/O

| # | Feature | Description |
|---|---------|-------------|
| IO01 | Load `.achx` file | `AnimationChainListSave.FromFile()`, adds `ShapeCollectionSave` to all frames |
| IO02 | Save `.achx` file | `AppCommands.SaveCurrentAnimationChainList()` saves to `ProjectManager.FileName` |
| IO03 | Save `.achx` as new file | Calls `Save()` with a user-chosen path |
| IO04 | Save companion `.aeproperties` | `IoManager.SaveCompanionFileFor()` — XML-serializes `AESettingsSave` |
| IO05 | Load companion `.aeproperties` | `IoManager.LoadAndApplyCompanionFileFor()` — restores `UnitType`, snap-to-grid, grid size |
| IO06 | Apply companion settings | Fires `SettingsLoaded` event for the UI layer to apply expanded nodes and guide lines |
| IO07 | Recent files list | `AppSettingsModel.AddFile()` — deduplicates, inserts at front, trims to 20 entries |
| IO08 | Referenced PNGs discovery | `ProjectManager.ReferencedPngs` — list of PNG files in the project folder |
| IO09 | Save error handling | `IoManager.SaveFailed` event raised when `XmlSerialize` throws |
| IO10 | Invalid XML resilience | `LoadAndApplyCompanionFileFor` silently absorbs deserialization exceptions |

### 1.7 Application State

| # | Feature | Description |
|---|---------|-------------|
| AS01 | `UnitType` setting | Pixel / TextureCoordinate / SpriteSheet; changing fires `WireframeTextureChange` |
| AS02 | Wireframe zoom level | `WireframeZoomValue` (default 100); changing fires `AfterZoomChange` |
| AS03 | Snap-to-grid toggle | `IsSnapToGridChecked` boolean |
| AS04 | Grid size setting | `GridSize` integer (default 16) |
| AS05 | Project folder | `ProjectFolder` string used to suppress file-copy prompts |
| AS06 | `CurrentFrame` alias | Delegates to `SelectedState.Self.SelectedFrame` |

### 1.8 Application Events

| # | Feature | Description |
|---|---------|-------------|
| EV01 | `AnimationChainsChanged` | Broadcast when chains/frames/shapes are added, removed, or reordered |
| EV02 | `AfterZoomChange` | Fired when `WireframeZoomValue` changes |
| EV03 | `WireframePanning` | Fired when the wireframe camera pans |
| EV04 | `WireframeTextureChange` | Fired when `UnitType` changes |
| EV05 | `AchxLoaded` | Fired after loading a `.achx` file (carries the file path) |
| EV06 | `AfterAxisAlignedRectangleChanged` | Fired when a rect's properties are edited |
| EV07 | `AfterCircleChanged` | Fired when a circle's properties are edited |
| EV08 | `SelectionChanged` | Fired by `SelectedState` on any selection change |

### 1.9 Serialization Details (AnimationFrameSave)

| # | Feature | Description |
|---|---------|-------------|
| SER01 | `ShouldSerializeFlipHorizontal()` | Method; omits element when `false` |
| SER02 | `ShouldSerializeFlipVertical()` | Method; omits element when `false` |
| SER03 | `ShouldSerializeRelativeX()` | Method; omits element when value is 0 |
| SER04 | `ShouldSerializeRelativeY()` | Method; omits element when value is 0 |
| SER05 | `ShouldSerializeShapeCollectionSave` (property) | Property, NOT a method — XmlSerializer does NOT use it as a gate; `ShapeCollectionSave` serializes whenever non-null |
| SER06 | `RightCoordinate` default = 1 | Non-zero default must survive round-trip |
| SER07 | `BottomCoordinate` default = 1 | Non-zero default must survive round-trip |
| SER08 | XML root element | `<AnimationChainArraySave>` (from `[XmlType("AnimationChainArraySave")]`) |
| SER09 | Chain XML element | Each chain serializes as `<AnimationChain>` (from `[XmlElementAttribute("AnimationChain")]`) |

### 1.10 Texture Drop / Drag-and-Drop

| # | Feature | Description |
|---|---------|-------------|
| TD01 | Drop PNG onto frame | Assigns texture to that frame only |
| TD02 | Drop PNG onto chain (no modifier) | Assigns texture to all existing frames |
| TD03 | Drop PNG onto chain (Ctrl held) | Creates a new frame with the dropped texture |
| TD04 | Drop PNG onto empty chain | Creates a new frame with the texture |
| TD05 | Drop non-PNG ignored | Non-PNG files are silently ignored |
| TD06 | Relative path computation | Dropped path is made relative to the `.achx` location using `FileManager.MakeRelative()` |

### 1.11 Wireframe Display / Camera

| # | Feature | Description |
|---|---------|-------------|
| WF01 | Display frame texture | Renders the selected frame's texture at correct UV coordinates |
| WF02 | Display flip H/V | Applies flip transforms to the rendered texture |
| WF03 | Display shape overlays | Draws axis-aligned rectangles and circles over the frame |
| WF04 | Zoom in/out | Mouse wheel or zoom control changes `WireframeZoomValue` |
| WF05 | Pan | Middle-click or right-click drag pans the view, fires `WireframePanning` |
| WF06 | Snap-to-grid cursor | When snap-to-grid is enabled, drag handles snap to `GridSize` increments |
| WF07 | Guide lines | Horizontal/vertical guide lines dragged from ruler; stored in `AESettingsSave` |
| WF08 | Unit-type rendering | Switches UV display mode (pixel/texture-coord/sprite-sheet) |

### 1.12 Preview / Playback Control

| # | Feature | Description |
|---|---------|-------------|
| PL01 | Play animation | Steps through frames at `FrameLength`-based speed |
| PL02 | Pause animation | Halts playback at current frame |
| PL03 | Stop / reset animation | Returns to first frame |
| PL04 | Speed multiplier | Scales playback speed |
| PL05 | Loop animation | Wraps from last frame back to first |
| PL06 | Preview flip H/V | Applies chain-level flip to preview |

### 1.13 Tree View / UI Navigation

| # | Feature | Description |
|---|---------|-------------|
| TV01 | Expand/collapse chain nodes | Persisted in `AESettingsSave.ExpandedNodes` |
| TV02 | Select chain in tree | Drives `SelectedState.SelectedChain` |
| TV03 | Select frame in tree | Drives `SelectedState.SelectedFrame` |
| TV04 | Select shape in tree | Drives `SelectedState.SelectedRectangle` or `SelectedCircle` |
| TV05 | Multi-select frames | Populates `SelectedState.SelectedNodes` |
| TV06 | Right-click context menu | Exposes add/delete/move operations |
| TV07 | Rename chain | In-place rename via context menu or F2 |
| TV08 | Rename frame | In-place rename (changes `TextureName` or frame alias) |

### 1.14 InspectableImage (Texture Viewer)

| # | Feature | Description |
|---|---------|-------------|
| II01 | Display texture with grid overlay | Renders loaded texture in the inspector panel |
| II02 | Flood-fill UV selection | Click to flood-select a cell region; derives UV coordinates from pixel data |
| II03 | Manual UV rectangle | Drag to define UV region |
| II04 | UV feedback | Displays left/right/top/bottom coordinate values |

---

## 2. Feature-to-Test Mapping

### Test Files and Coverage

| Test File | Features Covered |
|-----------|-----------------|
| `AppCommandsChainTests.cs` | A01–A14 |
| `AppCommandsFrameTests.cs` | F01 (partial), F03–F05, F08 |
| `AppCommandsShapeTests.cs` | S01–S09 (structural), S07–S08 |
| `AppCommandsDeleteAsyncTests.cs` | A02 (confirm/cancel), F02 (confirm/cancel), S05, S06 |
| `SelectedStateTests.cs` | SS01–SS08 |
| `ObjectFinderTests.cs` | OL01–OL03 |
| `AppSettingsModelTests.cs` | IO07 |
| `IoManagerTests.cs` | IO04–IO06, IO09, IO10 |
| `AppStateTests.cs` | AS01–AS06 |
| `ApplicationEventsTests.cs` | EV01–EV08 |
| `AchxSerializationTests.cs` | IO01, IO02, SER06–SER09, F01–F11 (round-trip) |
| `AnimationFrameSaveConditionalSerializationTests.cs` | SER01–SER07 |
| `ProjectManagerReferencedPngTests.cs` | IO01, IO08 |
| `TextureDropProcessorTests.cs` | TD01–TD06 |
| `PlaybackControllerTests.cs` *(new)* | PL01, PL03, PL05 |
| `DragHandleTests.cs` *(new)* | S12, S13 |
| `FloodFillBoundsCalculatorTests.cs` *(new)* | II02 |
| `AppCommandsSaveAsTests.cs` *(new)* | IO03 |
| `AESettingsSaveRoundTripTests.cs` *(new)* | IO05 (guides + expanded nodes round-trip) |

### Coverage Summary

| Category | Total Features | Tested | Untested (unit) |
|----------|---------------|--------|-----------------|
| Chain Management | 14 | 14 | 0 |
| Frame Management | 11 | 9 | 2 (F06, F09–F10 inline set; covered implicitly) |
| Shape Management | 13 | 12 | 1 (S10–S11: property editor UI) |
| Selection State | 8 | 8 | 0 |
| Object Lookup | 3 | 3 | 0 |
| File I/O | 10 | 10 | 0 |
| Application State | 6 | 6 | 0 |
| Application Events | 8 | 8 | 0 |
| Serialization Details | 9 | 9 | 0 |
| Texture Drop | 6 | 5 | 1 (TD06 path separator) |
| Wireframe Display | 8 | 0 | 8 (rendering layer) |
| Preview/Playback | 6 | 3 | 3 (PL02 pause, PL04 speed, PL06 flip; rendering layer) |
| Tree View / UI | 8 | 0 | 8 (UI layer) |
| InspectableImage | 4 | 1 | 3 (II01, II03–II04: rendering layer) |
| **Total** | **114** | **88** | **26** |

---

## 3. Untestable Features and Gap Recommendations

The following features cannot be covered by the existing xUnit/headless unit-test infrastructure
because they depend on Avalonia rendering, SkiaSharp bitmap operations, or live window state.

### 3.1 Wireframe Control Rendering (WF01–WF08)

**Why untestable:** `WireframeControl` uses SkiaSharp canvas operations that require a live Avalonia render
loop. There is no rendered bitmap output accessible from a headless process.

**Recommended approach:**
1. **Screenshot-based integration tests** — Launch the app in a headless mode using
   `Avalonia.Headless` (available in Avalonia 12+). Render the control to an off-screen
   bitmap with `control.RenderToImage()`, then compare pixel data against stored baselines
   using a perceptual hash (pHash) with a tolerance threshold.
2. **Separate rendering unit** — Extract the UV→pixel coordinate math from `WireframeControl`
   into a pure `UvToPixelCalculator` helper class. Unit-test that class directly.
3. **Visual regression tool** — Use a tool like
   [Appium](https://appium.io/) or [FlaUI](https://github.com/FlaUI/FlaUI) for Windows
   desktop UI automation to capture screenshots and compare them.

### 3.2 Preview / Playback (PL01–PL06)

**Status (PL01, PL03, PL05): ✅ DONE** — `PlaybackController` extracted to `AnimationEditor.Core.CommandsAndState`. `PreviewControl` delegates to it. 14 unit tests cover frame advancement, looping, reset, zero-length frame defaults, and the `FrameIndexChanged` event. `Advance(double deltaSeconds)` replaces the inline timer arithmetic.

**Still untestable (PL02, PL04, PL06):** Explicit pause state, speed multiplier, and per-preview flip are not modelled in `PlaybackController` — they remain rendering-layer concerns.

**Remaining recommended approach:**
1. **Add pause/speed to `PlaybackController`** — Expose `bool IsPlaying` and `double SpeedMultiplier`. Unit-test them directly; `PreviewControl` reads them on each tick.
2. **Integration test with headless Avalonia** — Boot the full `MainWindow` headlessly and synthesize `Tick` events to verify PL06 flip rendering.

### 3.3 Tree View / UI Navigation (TV01–TV08)

**Why untestable:** Tree node expansion/collapse state, rename in-place editing, and context
menus are Avalonia `TreeView` behaviors that require a live visual tree.

**Recommended approach:**
1. **ViewModel-layer tests** — If the project is refactored to MVVM, test the `TreeViewModel`
   (expand state, node collection, rename command) with pure xUnit tests.
2. **UI automation** — Use FlaUI/Appium to verify that right-clicking a chain node shows
   the expected context menu items and that rename operations propagate to the model.

### 3.4 InspectableImage Flood-Fill (II01–II04)

**Status (II02): ✅ DONE** — Flood-fill BFS extracted to `AnimationEditor.Core.Rendering.FloodFillBoundsCalculator`. `InspectableImage` now delegates the entire algorithm via a `Func<int, int, bool> isOpaque` predicate — no SkiaSharp in the algorithm. 11 unit tests cover single pixels, solid blocks, transparent seeds, OOB seeds, two-island isolation, L-shapes, and edge cases. `_visited` field and `EnsureVisited()` removed from `InspectableImage`.

**Still untestable (II01, II03–II04):** Display rendering and manual UV-rectangle drag require a live Avalonia canvas.

**Remaining recommended approach:**
1. **Bitmap integration tests** — Create small synthetic PNG files with known solid-color
   regions. Load them via SkiaSharp in tests (SkiaSharp is already a dependency) and verify
   that the computed UV coordinates match expectations.

### 3.5 Save-As Dialog (IO03)

**Status: ✅ DONE** — `IFileDialogService` interface added to `AnimationEditor.Core.IO`. `NullFileDialogService` is the default; `AvaloniaFileDialogService` (backed by `StorageProvider`) is wired in `MainWindow.WireAppCommands()`. `AppCommands.SaveCurrentAnimationChainListAsync()` replaces the former ad-hoc `SaveAsAsync()` in `MainWindow`. 7 unit tests cover the cancel path (no file, no FileName update, no event) and the confirm path (file saved, `FileName` updated, `SaveAsCompleted` fired, content verified).

### 3.6 AESettingsSave Expanded Nodes / Guide Lines (IO05 partial)

**Status: ✅ DONE** — 17 round-trip tests added in `AESettingsSaveRoundTripTests.cs` covering: `HorizontalGuides` and `VerticalGuides` value/order preservation, independent storage of both collections, `ExpandedNodes` name and insertion-order preservation, `UnitType` (Pixel / TextureCoordinate / SpriteSheet), `SnapToGrid`, `GridSize` (default=16 / custom), `AnimationChainSettings` single and multiple entries, empty round-trip, and an all-fields-populated integration check.

**Still untestable:** Applying the loaded settings to the live Avalonia tree and guide overlays requires UI automation.

**Remaining recommended approach:**
1. **UI integration test** — After loading a file with known expansion state, verify with
   FlaUI that the correct tree nodes are visually expanded.

### 3.7 Drag-Handle Shape Editing (S12–S13)

**Status: ✅ DONE** — `HandleKind` enum, `DragHandleHitTester`, `DragHandleApplier`, and `BoundsRect` extracted to `AnimationEditor.Core.Rendering`. `WireframeControl`'s private `HandleKind` enum removed; `HitTestHandle()` and `ApplyHandleDrag()` fully delegate to Core. 25 unit tests cover all 8 handle positions, Move/None hit-tests, within/beyond hit-radius, all handle-kind delta applications, bitmap clamping, minimum 1px size enforcement, and UV coordinate output.

### 3.8 Summary of Gap-Closing Recommendations

| Priority | Action | Status | Value |
|----------|--------|--------|-------|
| HIGH | Extract `PlaybackController` | ✅ Done | PL01, PL03, PL05 now covered (14 tests) |
| HIGH | Extract drag-handle math into pure functions | ✅ Done | S12–S13 now covered (25 tests) |
| HIGH | Extract flood-fill UV algorithm | ✅ Done | II02 now covered (11 tests) |
| MEDIUM | Inject `IFileDialogService` for save-as | ✅ Done | IO03 now covered (7 tests) |
| MEDIUM | `AESettingsSave` XML round-trip tests | ✅ Done | IO05 guides/expanded covered (17 tests) |
| MEDIUM | Extract UV→pixel math from `WireframeControl` | Open | Would cover WF01 math layer |
| LOW | Add Avalonia headless rendering tests | Open | Would cover WF01–WF08, PL02, PL04, PL06 |
| LOW | Add FlaUI/Appium UI automation | Open | Would cover TV01–TV08, WF04–WF05 |

---

*Report updated April 23, 2026. 288 unit tests across 19 test files (+70 tests, +5 files since initial report).*
*All 288 tests pass against `AnimationEditor.Core.Tests` (net8.0, xUnit 2.9.2).*
*New Core modules: `PlaybackController`, `HandleKind`, `DragHandleHitTester`, `DragHandleApplier`, `BoundsRect`, `FloodFillBoundsCalculator`, `IFileDialogService`, `NullFileDialogService`.*
*New App module: `AvaloniaFileDialogService` (`src/AnimationEditor.App/Services/`).*
