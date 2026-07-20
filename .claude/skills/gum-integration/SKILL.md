---
name: gum-integration
description: How FlatRedBall integrates the Gum UI layout/rendering library — plugin structure, .gumx projects, runtime wrapper. Triggers: GumPlugin, GraphicalUiElement, GUE, .gumx, .gusx, Gum.Wireframe, PositionedObjectGueWrapper, RenderingLibrary.
---

# Gum Integration

Gum is a **separate sibling repo** (`..\Gum` relative to this repo, e.g. `C:\Users\vchel\Documents\GitHub\Gum` in this checkout), not a submodule or NuGet package — FlatRedBall references its projects via relative `<ProjectReference>` paths (e.g. `FRBDK\Glue\GumPlugin\GumPlugin\GumPlugin.csproj` → `..\..\..\..\..\Gum\GumCommon\GumCommon.csproj`). Gum is FRB's UI layout/rendering library (screens, components, states, data binding) — general Gum behavior/API questions belong to that repo, not this skill.

## Where things live

| Piece | Location |
|---|---|
| Gum's core runtime element (`GraphicalUiElement`, `Gum.Wireframe` namespace) | sibling `Gum\GumRuntime` |
| Gum's low-level rendering/camera abstractions (`RenderingLibrary` namespace — `Camera`, `SystemManagers`, `LayerCameraSettings`) | sibling `Gum\RenderingLibrary` |
| Other Gum sub-libraries (data types, MonoGame/Skia backends) | sibling `Gum\GumCore`, `GumCommon`, `GumDataTypes`, `MonoGameGum`, `SkiaGum*` |
| Glue's Gum plugin | `FRBDK\Glue\GumPlugin\GumPlugin\` |
| FRB-side integration/test shims (not the real Gum source) | `Engines\SkiaGum`, `Tests\EngineUnitTests\Gum` |

Glue's Gum plugin has the same "master template → generated copy" split as the [[glue-live-edit]] skill: `Embedded\*.cs` files (`PositionedObjectGueWrapper.cs`, `GraphicalUiElement.Binding.cs`, `GraphicalUiElement.IWindow.cs`, `SystemManagers.FlatRedBall.cs`, `ContentManagerWrapper.cs`) are hand-edited sources copied verbatim into generated game projects — not generator *output*, actual C# that ships as-is. `CodeGeneration\*` is the real generator side: `GueDerivingClassCodeGenerator.cs` (per-Screen/Component derived class), `GumGame1CodeGenerator.cs` (Game1 wiring), `StandardsCodeGenerator.cs` + per-type generators (`NineSliceCodeGenerator.cs`, `TextCodeGenerator.cs`...) and `StateCodeGenerator*.cs` for the property/state pipelines documented in the [[gum-codegen]] skill, `FormsClassCodeGenerator.cs`/`FormsObjectCodeGenerator.cs` for Gum Forms controls.

## The .gumx project

Gum UI data lives in its **own file format alongside** `.glux`/`.gluj`, referenced by relative path from the Glue project (e.g. a `.gluj`'s `"Name": "GumProject/GumProject.gumx"`). One `.gumx` (project index, `GumProjectSave` XML) plus one file per element under `Content\GumProject\`: `.gusx` (Screens/Components), `.behx` (behaviors), `.ganx` (animations). `GumProjectManager.cs`/`FileChangeManager.cs` (`GumPlugin\Managers`) load it and watch for external edits (e.g. from the standalone Gum tool).

## Runtime bridging

`PositionedObjectGueWrapper : PositionedObject` (`GumPlugin\Embedded\PositionedObjectGueWrapper.cs`) wraps a `GraphicalUiElement` inside FRB's `PositionedObject` hierarchy, so Gum UI can attach to/move with an FRB entity like any other positioned object. Top-level Screens/Components loaded standalone instead get their own generated derived class from `GueDerivingClassCodeGenerator.cs`, extending `GraphicalUiElement` directly rather than going through the wrapper.

## Per-Screen Gum screen (auto-created)

Adding an FRB Screen in Glue auto-creates a paired Gum screen so anything placed on it shows up automatically when the FRB Screen loads — you don't need to know this exists until you go looking for "where did `GameScreenGum` come from."

- Hook: FRB-screen-created event → `GumPluginCommands.AddScreenForGlueScreen` (`GumPlugin\Managers\GumPluginCommands.cs:199-226`), also triggerable by hand via right-click (`RightClickManager.cs:92`).
- **Naming convention**: strip the FRB screen's `Screens/` prefix and append `Gum` — `Screens/GameScreen` → Gum screen `GameScreenGum` (`GetExpectedGumScreenNameFor`, `GumPluginCommands.cs:187-197`).
- It's created only if missing, then linked onto the FRB Screen as a normal `NamedObjectSave`/file reference (`RightClickManager.AddGumScreenScreenByName`) — there's no special runtime loading path. Generated codegen (`GueDerivingClassCodeGenerator.cs:890-926`) emits the same `<name>.AddToManagers(...)` call it would for any other Gum object, so the paired screen is added/removed on FRB Screen load/unload exactly like every other object on that Screen.

## World-space Gum objects (health bars, icons, in-world dialog)

Placing a Gum object at fixed screen-space UI coordinates is the default. To have it track an FRB entity's world position instead (health bar over an enemy, lock icon over a door), Glue exposes **`NamedObjectSave.AttachToContainer`** — a bool property under the object's "Creation" category, only shown when the object's container is an `EntitySave` and it isn't a list (`NamedObjectPropertyGridDisplayer.cs:330-338`). A sibling property, **`AttachToCamera`** (shown only for Screen-level objects, not Entity), sticks a Gum object to the camera instead of the world.

This is **not** a plain FRB-attach and **not** pure coordinate conversion — it's a hybrid, and the mechanism matters if you're debugging drift:

- When `AttachToContainer` is true on an Entity, codegen wraps the GUE in `PositionedObjectGueWrapper(this, gumObject)` (`GueDerivingClassCodeGenerator.cs:900-919`), added via `SpriteManager.AddPositionedObject`. The wrapper itself calls `AttachTo(frbObject)` (`Embedded\PositionedObjectGueWrapper.cs:52`) so it inherits the entity's world position/rotation through FRB's normal attachment tree — **but** the actual `GraphicalUiElement` is reparented under an intermediary invisible `GumParent` GUE (`gumObject.Parent = GumParent`, line 69), not attached directly.
- Every frame (`UpdateGumObject`, lines 95-170): compute `worldPosition = FrbObject.Position + RelativePosition`, convert to screen pixels via `MathFunctions.AbsoluteToWindow(...)` using `Camera.Main`, then re-derive Gum canvas coordinates as a *ratio* of that screen position to `camera.DestinationRectangle.Width/Height`, scaled by `GraphicalUiElement.CanvasWidth/CanvasHeight` (lines 108-141) — this is the same ratio-based conversion pattern used for edit-mode zoom in [[glue-live-edit]], not a one-shot pixel offset, so it stays correct as the camera pans/zooms.
- `GumParent.Width/Height` mirror the FRB object's `ScaleX/ScaleY * 2` when it implements `IReadOnlyScalable`; `GumParent.Visible` mirrors `AbsoluteVisible` (lines 154-169).

**Known landmines (straight from code comments, not inferred):**
- **Position tracks the camera zoom; the Gum object's own visual *scale* does not.** The wrapper's author left a comment calling this code "a huge mess" while iterating on it — expect a world-space Gum object to move to the right place when zoomed but not grow/shrink with it.
- **No multiple-camera/multiple-layer support** — explicitly flagged `// todo - need to support multiple cameras and layers` (line 101). If a project has split-screen or multiple FRB layers with independent cameras, world-space attachment is not guaranteed correct.
- **3D/perspective cameras are unimplemented** — stubbed `// todo - need to figure out 3D` (line 144); the position math assumes an orthogonal camera.
- **No dedicated "world-space" Gum Layer type.** World-space vs screen-space is purely: which FRB `Layer` the entity/object belongs to, plus whether `AttachToContainer` is set — not a distinct kind of Gum `Layer`. Gum `Layer`s are generated 1:1 per FRB `Layer` (`GumLayerCodeGenerator.cs`/`GumLayerAssociationCodeGenerator.cs`).
- **Collision follows the wrapper, not the raw GUE.** With `AttachToContainer` set, collision is wired through `GumCollidableExtensions.AddCollision(this, wrapperForAttachment)` (`GumCollidableCodeGenerator.cs:39`) — custom collision code must go through `PositionedObjectGueWrapper`, not the `GraphicalUiElement` directly, or shapes won't track the attached position.

## Vocabulary

- **GraphicalUiElement (GUE)** — Gum's core runtime UI node; owns position/size/children/rendering.
- **Screen** (`.gusx`) — top-level Gum layout, analogous to an FRB Screen.
- **Component** (`.gusx`) — reusable, instantiable UI element (like a prefab/control).
- **Standard element** — Gum's built-in primitive types (Text, Sprite/NineSlice, Container, etc.), generated by `StandardsCodeGenerator.cs`.
- **Instance** — a placed occurrence of a Component/Standard element inside a Screen/Component.
- **State / Variable** — named property sets swappable at runtime; see [[gum-codegen]] for how Glue generates their skip/gating logic.

## Related skills

- [[gum-codegen]] — the version-gating skip-list pipelines for standard-element property/state generation (narrower, Glue-only).
- [[glue-live-edit]] — covers Gum's dual-zoom sync (`Camera.Main` vs `Renderer.Camera.Zoom`/`LayerCameraSettings.Zoom`) and other edit-mode-specific Gum behavior.
- [[gum-shared-source]] — how FRB's `.csproj`s pull in Gum/MonoGameGum source files individually rather than sharing a project, and where the two diverge (e.g. `Cursor`).
