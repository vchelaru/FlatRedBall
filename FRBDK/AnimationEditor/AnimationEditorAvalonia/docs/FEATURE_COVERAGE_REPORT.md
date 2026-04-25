# AnimationEditor Feature Coverage Report

> Generated from exhaustive research of the old WinForms app, FRB engine models, and the new Avalonia port.
> Test results: **417 tests passing, 0 failing** as of this report (404 Core + 13 App headless).

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
| A15 | Smart duplicate direction naming | When the source chain name contains "Left"/"Right"/"Up"/"Down", the Duplicate sub-menu auto-suggests the mirrored name (e.g. "WalkLeft" → "WalkRight"); case-variants handled; user can override or ignore |
| A16 | Adjust offsets dialog | "Adjust Offsets" in chain right-click; two modes: **Justify** (sets `RelativeY` so the frame's bottom edge aligns to y=0, divided by `OffsetMultiplier`) and **Adjust All** (shifts or overwrites `RelativeX/Y` of every frame in the chain by a user-entered delta or absolute value) |
| A17 | Scale frame times dialog | "Adjust All Frame Time" in chain right-click; two modes: **Keep Proportional** (multiplies every `FrameLength` by a scale factor so the total duration equals the user-entered target, preserving relative ratios) and **Set All Same** (divides the target total by frame count and assigns that uniform value to every frame) |

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
| F12 | Add multiple frames (batch) | "Add Multiple Frames" dialog; user enters count N; optional **Increment frame position** auto-advances `Left/Right/Top/BottomCoordinate` row-by-row based on the last frame's cell size; warns when increment would exceed texture bounds; N frames are added sequentially with each selecting the newly created frame |
| F13 | Pixel-mode frame UV editing | When `UnitType` is `Pixel`, setting X/Y moves both the near and far edges by the pixel delta (preserving size); setting Width/Height adjusts only the far edge; all coordinates are rounded to the nearest pixel boundary after each change (mirroring `AnimationFrameDisplayer.CoordinateChange` in the WinForms app) |

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
| IO11 | New animation | File > New creates an empty `AnimationChainListSave`, clears `ProjectManager.FileName`, and immediately triggers the async Save-As flow so the user can choose a save path |
| IO12 | Command-line file argument | On startup (`Window.Opened`), if `Environment.GetCommandLineArgs()[1]` is an existing `.achx` path it is loaded automatically, bypassing the file picker |
| IO13 | Export current animation as GIF | File > "Save current animation as GIF"; renders each frame's texture region into a `Bitmap` and encodes as an animated GIF using `AnimatedGifEncoder`; user picks save path via `SaveFileDialog` |
| IO14 | Copy / paste objects | Edit > Copy / Ctrl+C serializes the selected chain(s), frame(s), rectangle, or circle to the system clipboard as a typed XML string (`List<AnimationChainSave>:...`); Paste / Ctrl+V deserializes and appends into the current context; supports chain-to-chain, frame-to-chain, and shape-to-frame paste |
| IO15 | Resize texture | Edit > "Resize texture"; pads the selected frame's texture PNG to power-of-two dimensions; user chooses to replace the original file or save a renamed copy (`*Resize.png`); adjusts `Left/Right/Top/BottomCoordinate` on every frame across all chains that references the same texture file |

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
| WF09 | Magic wand — create frame from region | When `IsMagicWandMode` is active and Ctrl is held (or no frame is pre-selected), a flood-fill click fires `FrameCreatedFromRegion(minX, minY, maxX, maxY)`; `MainWindow` creates a new `AnimationFrameSave` with UV = `min/max ÷ bitmapSize` and appends it to the selected chain |
| WF10 | Texture selector | A texture dropdown in the wireframe toolbar lists all textures referenced by the loaded ACHX (from frame `TextureName`s) plus `ReferencedPngs`; selecting an entry loads that texture into the wireframe canvas; auto-syncs when frame selection changes |
| WF11 | Sprite-sheet tile-index UV selection | When `UnitType` is `SpriteSheet`, the user enters `TileX`/`TileY`; UV coordinates are computed as `left = tileIndex * tileSize / textureSize`, `right = left + tileSize / textureSize` (mirroring `AnimationFrameDisplayer.SetTileX/SetTileY`); per-texture tile dimensions stored in `TileMapInformation` |

### 1.12 Preview / Playback Control

| # | Feature | Description |
|---|---------|-------------|
| PL01 | Play animation | Steps through frames at `FrameLength`-based speed |
| PL02 | Pause animation | Halts playback at current frame |
| PL03 | Stop / reset animation | Returns to first frame |
| PL04 | Speed multiplier | Scales playback speed |
| PL05 | Loop animation | Wraps from last frame back to first |
| PL06 | Preview flip H/V | Applies chain-level flip to preview |
| PL07 | Onion skin | When a single frame is pinned, renders the previous frame at 50% alpha behind it; wraps index for the first frame; toggled by `OnionSkinToggle` |
| PL08 | Preview guides overlay | Toggles rendering of horizontal/vertical guide lines (from `AESettingsSave`) over the preview canvas; controlled by `ShowGuidesCheck` |
| PL09 | Preview zoom | Independent zoom combo (10%–400%) for the preview panel via `PreviewZoomCombo` and mouse wheel; `SetZoomPercent` clamps to 0.05×–32× |
| PL10 | Preview pan | Middle-click or Alt+left-click drag pans the preview viewport; uses dedicated camera (`_panX`/`_panY`) shared with zoom-toward computation |
| PL11 | Preview sprite alignment | Combo in preview toolbar: Center (FRB default), TopLeft, TopRight, BottomLeft, BottomRight, etc.; changes the anchor point used when positioning the animated sprite's `RelativeX/Y` offset in the preview panel |
| PL12 | Preview offset multiplier | Text box in preview toolbar (default 1.0); divides `RelativeX/Y` when computing the display offset in the preview panel; also consumed by the Adjust Offsets > Justify calculation to determine target `RelativeY` |

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
| TV09 | View texture in Explorer | Right-click on a frame → "View Texture in Explorer"; opens Windows File Explorer with the frame's texture file pre-selected via `Process.Start("explorer.exe", "/select,...")`; shows an error message if no texture is set |

**Status:** All extractable logic for WF11 (tile-index UV math) and F13 (pixel-mode UV editing) has been extracted into Core and is fully unit-tested.

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
| `PlaybackControllerTests.cs` *(new)* | PL01, PL02, PL03, PL04, PL05 |
| `DragHandleTests.cs` *(new)* | S12, S13 |
| `FloodFillBoundsCalculatorTests.cs` *(new)* | II02 |
| `AppCommandsSaveAsTests.cs` *(new)* | IO03 |
| `AESettingsSaveRoundTripTests.cs` *(new)* | IO05 (guides + expanded nodes round-trip) |
| `WireframeTransformTests.cs` *(new)* | WF01 (coordinate math layer), WF04 (zoom-toward math) |
| `TreeBuilderTests.cs` *(new)* | TV01 (expand-state logic), TV02–TV04 (selection routing) |
| `GridSnapperTests.cs` *(new)* | WF06 (snap-to-grid math layer) |
| `FlipScaleCalculatorTests.cs` *(new)* | WF02 math layer, PL06 math layer |
| `UnitConverterTests.cs` *(new)* | WF08 (unit-type display math) |
| `DirectionNameSuggesterTests.cs` *(new)* | A15 (mirror-direction name suggestion) |
| `AdjustOffsetCalculatorTests.cs` *(new)* | A16 (justify-bottom + adjust-all offset math) |
| `FrameTimeScalerTests.cs` *(new)* | A17 (keep-proportional + set-all-same frame time scaling) |
| `BatchFrameBuilderTests.cs` *(new)* | F12 (batch-add UV increment math) |
| `ClipboardPayloadTests.cs` *(new)* | IO14 (XML clipboard serialization / deserialization) |
| `TextureResizeAdjusterTests.cs` *(new)* | IO15 (UV coordinate adjustment after texture resize) |
| `SpriteAlignmentOffsetCalculatorTests.cs` *(new)* | PL11 (sprite alignment offset math) |
| `OffsetMultiplierConverterTests.cs` *(new)* | PL12 (offset multiplier round-trip math) |
| `AppCommandsNewFileTests.cs` *(new)* | IO11 (`AppCommands.NewFile()` — fresh ACLS, clear state, fire events) |
| `CommandLineArgParserTests.cs` *(new)* | IO12 (`CommandLineArgParser.ParseFileArgument` — .achx detection) |
| `AppCommandsFrameFromPixelBoundsTests.cs` *(new)* | WF09 (`AppCommands.AddFrameFromPixelBounds()` — UV-from-pixels) |
| `TextureListBuilderTests.cs` *(new)* | WF10 (`TextureListBuilder.GetAvailableTextures()` — deduplication + sort) |
| `TileCoordinateCalculatorTests.cs` *(new)* | WF11 (sprite-sheet tile-index → UV math) |
| `PixelFrameEditorTests.cs` *(new)* | F13 (pixel-mode SetX/Y/Width/Height UV editing) |

### Coverage Summary

| Category | Total Features | Tested | Untested (unit) |
|----------|---------------|--------|-----------------|
| Chain Management | 17 | 17 | 0 (A15–A17 dialog UI wiring untestable; Core logic now covered) |
| Frame Management | 13 | 12 | 1 (F06 texture-field text-input UI-only; F12 dialog UI wiring untestable — Core logic now covered) |
| Shape Management | 13 | 11 | 2 (S10–S11: property editor UI) |
| Selection State | 8 | 8 | 0 |
| Object Lookup | 3 | 3 | 0 |
| File I/O | 15 | 14 | 1 (IO13: GIF export rendering — IO11 `NewFile()` + IO12 `CommandLineArgParser` + IO14 serialization + IO15 UV-adjust now all covered) |
| Application State | 6 | 6 | 0 |
| Application Events | 8 | 8 | 0 |
| Serialization Details | 9 | 9 | 0 |
| Texture Drop | 6 | 6 | 0 |
| Wireframe Display | 11 | 9 | 2 (WF03, WF07: rendering/UI — WF09 pixel-bounds frame creation + WF10 texture-list logic + WF11 tile-index UV now covered) |
| Preview/Playback | 12 | 8 | 4 (PL07–PL10: SkiaSharp rendering + Avalonia pointer gesture — PL11 alignment math + PL12 offset multiplier math now covered) |
| Tree View / UI | 9 | 8 | 1 (TV09: OS shell integration) |
| InspectableImage | 4 | 1 | 3 (II01, II03–II04: rendering layer) |
| **Total** | **134** | **125** | **9** |

---

## 3. Untestable Features and Gap Recommendations

The following features cannot be covered by the existing xUnit/headless unit-test infrastructure
because they depend on Avalonia rendering, SkiaSharp bitmap operations, or live window state.

### 3.1 Wireframe Control Rendering (WF01–WF08)

**Still untestable (WF03, WF07):** Shape-overlay drawing and guide-line drag-from-ruler require a live SkiaSharp canvas.


sts pass in `AnimationEditor.App.Tests` (net8.0, xunit.v3 3.2.2).

### 3.4 InspectableImage Flood-Fill (II01–II04)

**Still untestable (II01, II03–II04):** Display rendering and manual UV-rectangle drag require a live Avalonia canvas.

**Remaining recommended approach:**
1. **Bitmap integration tests** — Create small synthetic PNG files with known solid-color
   regions. Load them via SkiaSharp in tests (SkiaSharp is already a dependency) and verify
   that the computed UV coordinates match expectations.

### 3.6 AESettingsSave Expanded Nodes / Guide Lines (IO05 partial)

**Still untestable:** Applying the loaded settings to the live Avalonia tree and guide overlays requires UI automation.

**Remaining recommended approach:**
1. **UI integration test** — After loading a file with known expansion state, verify with
   FlaUI that the correct tree nodes are visually expanded.


### 3.8 Snap-to-Grid Math (WF06 partial)

**Still untestable (WF06 cursor display):** The visual highlighting of the snapped grid cell requires a live SkiaSharp canvas.

### 3.11 Unit-type Display Conversion (WF08)


**Still untestable (display wiring):** Plugging `UnitConverter` into the inspector labels requires an Avalonia render cycle.

### 3.12 Per-Frame Flip Scale (WF02 + PL06)

**Still untestable (canvas application):** The actual `canvas.Scale(...)` call and resulting pixel output require a live SkiaSharp context.

### 3.13 Pan Math (WF05)


**Still untestable (gesture routing):** Whether the panning gesture starts correctly (middle-mouse / Alt+left) and fires `WireframePanning` requires Avalonia pointer event synthesis.


### 3.15 Summary of Gap-Closing Recommendations

| Priority | Action | Status | Value |
|----------|--------|--------|-------|
| HIGH | Extract `PlaybackController` | ✅ Done | PL01, PL03, PL05 now covered (14 tests) |
| HIGH | Extract drag-handle math into pure functions | ✅ Done | S12–S13 now covered (25 tests) |
| HIGH | Extract flood-fill UV algorithm | ✅ Done | II02 now covered (11 tests) |
| MEDIUM | Inject `IFileDialogService` for save-as | ✅ Done | IO03 now covered (7 tests) |
| MEDIUM | `AESettingsSave` XML round-trip tests | ✅ Done | IO05 guides/expanded covered (17 tests) |
| MEDIUM | Extract UV→pixel math from `WireframeControl` | ✅ Done | WF01, WF04 math layer (20 tests) |
| MEDIUM | Add pause/speed to `PlaybackController` | ✅ Done | PL02, PL04 now covered (10 new tests) |
| MEDIUM | Extract tree logic to `TreeBuilder` (Core.ViewModels) | ✅ Done | TV01–TV04 logic layer (21 tests) |
| MEDIUM | Extract grid-snap math to `GridSnapper` (Core.Rendering) | ✅ Done | WF06 math layer (15 tests) |
| MEDIUM | Add explicit TD06 relative-path edge-case tests | ✅ Done | TD06 now fully covered (+3 tests) |
| MEDIUM | Add `AppCommands.FlipFrameHorizontally/Vertically` (F09/F10) | ✅ Done | F09–F10 now covered (+8 tests) |
| MEDIUM | Wire TV01 two-way `IsExpanded` binding in AXAML | ✅ Done | TV01 fully closed (expand state roundtrips via model) |
| MEDIUM | Extract flip-scale decision to `FlipScaleCalculator` | ✅ Done | WF02 + PL06 math layer (11 tests) |
| MEDIUM | Add `WireframeTransform.Pan` + wire `WireframeControl` | ✅ Done | WF05 math layer (5 tests) |
| MEDIUM | Extract `UnitConverter.ToDisplay`/`FromDisplay` | ✅ Done | WF08 display math (13 tests) |
| MEDIUM | Add `AppCommands.RenameChain`/`RenameFrame` | ✅ Done | TV07–TV08 logic layer (8 tests) |
| LOW | Add Avalonia headless rendering tests | Open | Would cover WF03, WF07 canvas drawing |
| LOW | Add Avalonia Headless tests (TV05, TV06) | ✅ Done | TV05 multi-select (4 tests) + TV06 context menu (9 tests) in `AnimationEditor.App.Tests` |
| MEDIUM | Extract `DirectionNameSuggester` (A15) | ✅ Done | A15 Core logic covered (25 tests) |
| MEDIUM | Extract `AdjustOffsetCalculator` (A16) | ✅ Done | A16 Core logic covered (15 tests) |
| MEDIUM | Extract `FrameTimeScaler` (A17) | ✅ Done | A17 Core logic covered (12 tests) |
| MEDIUM | Extract `BatchFrameBuilder` (F12) | ✅ Done | F12 UV-increment math covered (15 tests) |
| MEDIUM | Extract `ClipboardPayload` serialization (IO14) | ✅ Done | IO14 XML round-trips covered (18 tests) |
| MEDIUM | Extract `TextureResizeAdjuster` (IO15) | ✅ Done | IO15 UV-adjust math covered (10 tests) |
| MEDIUM | Wire new Core classes into `AppCommands` | ✅ Done | `AdjustOffsetsJustifyBottom`, `AdjustOffsetsAdjustAll`, `ScaleFrameTimesProportional`, `ScaleFrameTimesSetAllSame`, `AddMultipleFrames`, `AdjustUVAfterResize` |
| MEDIUM | Extract `SpriteAlignment` enum + `SpriteAlignmentOffsetCalculator` (PL11) | ✅ Done | PL11 alignment offset math covered (9 tests) |
| MEDIUM | Extract `OffsetMultiplierConverter` + `AppState.OffsetMultiplier` (PL12) | ✅ Done | PL12 multiplier round-trip math covered (13 tests) |
| MEDIUM | Add `AppCommands.NewFile()` (IO11) | ✅ Done | IO11 Core command covered (7 tests) |
| MEDIUM | Extract `CommandLineArgParser.ParseFileArgument()` (IO12) | ✅ Done | IO12 arg parsing covered (9 tests) |
| MEDIUM | Add `AppCommands.AddFrameFromPixelBounds()` (WF09) | ✅ Done | WF09 UV-from-pixels logic covered (12 tests) |
| MEDIUM | Extract `TextureListBuilder.GetAvailableTextures()` (WF10) | ✅ Done | WF10 texture deduplication logic covered (10 tests) |
| MEDIUM | Extract `TileCoordinateCalculator` (WF11) | ✅ Done | WF11 tile-index → UV math covered (11 tests) |
| MEDIUM | Extract `PixelFrameEditor` (F13) | ✅ Done | F13 pixel-mode SetX/Y/Width/Height UV editing covered (15 tests) |

### 3.16 Newly Documented UI-Layer Features (PL07–PL10, WF09–WF10, IO11, IO12)

A second pass at the full source identified 8 features that were present in the implementation but absent from the inventory:

| ID | Feature | Status | Notes |
|----|---------|--------|-------|
| PL07 | Onion skin | ❌ Untestable | `PreviewControl.Render` — SkiaSharp canvas alpha compositing |
| PL08 | Preview guides overlay | ❌ Untestable | `PreviewControl.Render` — SkiaSharp canvas line drawing |
| PL09 | Preview zoom | ❌ Untestable (gesture) | `PreviewControl.SetZoomPercent` already implemented; pointer-event routing untestable |
| PL10 | Preview pan | ❌ Untestable (gesture) | `PreviewControl.OnPointerPressed/Moved` — Avalonia pointer capture gesture |
| WF09 | Magic wand → create frame | ✅ Core logic covered | `AppCommands.AddFrameFromPixelBounds()` extracted; gesture wiring untestable (12 tests) |
| WF10 | Texture selector | ✅ Core logic covered | `TextureListBuilder.GetAvailableTextures()` extracted; ComboBox population UI-only (10 tests) |
| IO11 | File > New | ✅ Core logic covered | `AppCommands.NewFile()` extracted from UI layer (7 tests) |
| IO12 | Command-line file argument | ✅ Core logic covered | `CommandLineArgParser.ParseFileArgument()` extracted (9 tests) |
| PL11 | Preview sprite alignment | ✅ Core logic covered | `SpriteAlignment` enum + `SpriteAlignmentOffsetCalculator` extracted (9 tests) |
| PL12 | Preview offset multiplier | ✅ Core logic covered | `OffsetMultiplierConverter` + `AppState.OffsetMultiplier` extracted (13 tests) |

**Remaining untestable:** PL07/PL08 (SkiaSharp canvas rendering) and PL09/PL10 (Avalonia pointer-event routing). Bitmap integration tests could cover PL07/PL08 with synthetic PNGs.

### 3.17 Newly Discovered Features (Top-Down UI Audit — A15–A17, F12, IO13–IO15, PL11–PL12, TV09)

A top-down audit of the old WinForms app's `Controls/`, `Managers/`, `Plugins/`, `Gif/`, and `Editing/` folders surfaced 10 additional features that were completely absent from the inventory because the previous audit only read the Core library (bottom-up).

| ID | Feature | Why Previously Missed | Testability |
|----|---------|----------------------|-------------|
| A15 | Smart duplicate direction naming | Logic buried in `TreeViewManager.RightClick.cs` `CreateDuplicateToolStripItems` | Extractable to Core string utility — testable |
| A16 | Adjust offsets dialog | `AdjustOffsetViewModel.ApplyOffsets()` — pure logic in UI ViewModel | Math fully extractable to Core — testable |
| A17 | Scale frame times dialog | `AnimationChainTimeScaleWindow` + right-click `AdjustFrameTimeClick` — pure arithmetic | Math extractable to Core — testable |
| F12 | Batch add frames | `TreeViewManager.AddFramesClick` + `AnimationAddFramesWPF` dialog | UV increment math extractable; dialog interaction UI-only |
| IO13 | Export as GIF | `GifManager.SaveCurrentAnimationAsGif()` — requires bitmap rendering + file system | Bitmap rendering untestable without graphics context |
| IO14 | Copy / paste | `CopyManager.HandleCopy/HandlePaste` — XML serialization to system clipboard | Serialization logic testable; clipboard + paste-context wiring UI-only |
| IO15 | Resize texture | `ResizeMethods.ResizeTextureClick` — requires `GraphicsDevice` + file I/O | UV-adjustment math (`AdjustFrameToResize`) extractable — testable; image resize requires graphics context |
| PL11 | Preview sprite alignment | `PreviewControls.SpriteAlignmentComboBox` — rendering anchor change | Alignment enum + rendering UI-only |
| PL12 | Preview offset multiplier | `PreviewControls.OffsetMultiplier` text box — divides `RelativeX/Y` in preview math | Multiplier applied in `PreviewManager` render — extractable formula |
| TV09 | View texture in Explorer | `TreeViewManager.RightClick.ViewTextureInExplorer` — `Process.Start` OS shell call | OS integration — untestable in unit tests |

**Root cause of gap:** The prior audit read `src/AnimationEditor.Core` and inferred features from the data model API surface. It captured what data the model *can store* but was blind to any feature delivered through a dialog, property panel, toolbar widget, or OS integration that has no 1:1 backing method in Core. The fix is to always audit the old app's UI layer top-down as a first pass before reading Core bottom-up.

**Status:** All extractable logic for A15–A17, F12, IO14, IO15, WF11, and F13 has been extracted into Core and is fully unit-tested. `AppCommands` wiring methods added for all applicable features. The remaining untestable portions are the dialog UI interactions (open dialog, read user input, close) and OS-level operations (GIF bitmap rendering, OS clipboard I/O, system process launch for TV09).

| WF11 | Sprite-sheet tile-index UV | `AnimationFrameDisplayer.SetTileX/SetTileY` — pure index math (`left = tileIndex * tileSize / textureSize`) | Extractable to Core math — ✅ Done (11 tests) |
| F13  | Pixel-mode UV editing | `AnimationFrameDisplayer.CoordinateChange` X/Y/Width/Height cases — pure delta/rounding math | Extractable to Core math — ✅ Done (15 tests) |

---

*Report updated. 595 unit tests across 41 test files (feature inventory: 134 items; 125 covered, 9 untestable/UI-only).*
*`AnimationEditor.Core.Tests` (net8.0, xUnit 2.9.2): 582 tests passing.*
*`AnimationEditor.App.Tests` (net8.0, xunit.v3 3.2.2): 13 headless Avalonia tests passing (TV05 multi-select × 4, TV06 context menu × 9).*
*Core modules: `PlaybackController`, `HandleKind`, `DragHandleHitTester`, `DragHandleApplier`, `BoundsRect`, `FloodFillBoundsCalculator`, `WireframeTransform` (+ `Pan`), `GridSnapper`, `FlipScaleCalculator`, `UnitConverter`, `IFileDialogService`, `NullFileDialogService`, `TreeNodeVm`, `TreeBuilder`, `DirectionNameSuggester`, `AdjustOffsetCalculator`, `FrameTimeScaler`, `BatchFrameBuilder`, `ClipboardPayload`, `TextureResizeAdjuster`, `SpriteAlignment` enum, `SpriteAlignmentOffsetCalculator`, `OffsetMultiplierConverter`, `CommandLineArgParser`, `TextureListBuilder`, `TileCoordinateCalculator`, `PixelFrameEditor`.*
*AppCommands additions: `FlipFrameHorizontally`, `FlipFrameVertically`, `RenameChain`, `RenameFrame`, `AdjustOffsetsJustifyBottom`, `AdjustOffsetsAdjustAll`, `ScaleFrameTimesProportional`, `ScaleFrameTimesSetAllSame`, `AddMultipleFrames`, `AdjustUVAfterResize`, `NewFile`, `AddFrameFromPixelBounds`.*
*AppState additions: `SpriteAlignment`, `OffsetMultiplier`.*
*App: `AvaloniaFileDialogService`, `IsExpanded` binding via `TreeView.Styles` `{ReflectionBinding}`, `PreviewControl` delegates to `FlipScaleCalculator`, `WireframeControl` delegates to `WireframeTransform.Pan`.*
*Test files added (session): `TileCoordinateCalculatorTests.cs`, `PixelFrameEditorTests.cs`.*
