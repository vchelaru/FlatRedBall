---
name: achx-format
description: The .achx animation file format and its save/runtime split. Triggers: .achx, AnimationChainListSave, AnimationChainSave, AnimationFrameSave, ToAnimationChainList, FromXElement.
---

# .achx Animation File Format

`.achx` is the XML serialization of an `AnimationChainListSave`. It describes texture-flip animations: a list of chains, each a list of frames, each frame naming a texture + UV/pixel coordinates + timing (and, more recently, optional per-frame color). See the [AnimationChainList file docs](https://docs.flatredball.com/flatredball/glue-reference/files/file-types/glue-reference-animationchainlist) and the [runtime API](https://docs.flatredball.com/flatredball/api/flatredball/graphics/animation/flatredball-graphics-animationchainlist).

## The save ↔ runtime split (the core thing to internalize)

There are **two parallel class trees** — a serialized "Save" tree and a runtime tree — and you convert between them. Don't confuse the two.

| Save (`.achx`, `Content/AnimationChain/`) | Runtime (`Graphics/Animation/`) |
|---|---|
| `AnimationChainListSave` (`[XmlType("AnimationChainArraySave")]`) | `AnimationChainList` |
| `AnimationChainSave` (`[XmlRoot("AnimationChain")]`) | `AnimationChain` |
| `AnimationFrameSave` | `AnimationFrame` |

- **Load:** `AnimationChainListSave.FromFile(fileName)` → `ToAnimationChainList(...)` → per chain `ToAnimationChain(...)` → per frame `AnimationFrameSave.ToAnimationFrame(...)`.
- **Save:** the `AnimationFrameSave(AnimationFrame template)` ctor copies runtime → save.
- **Apply to a Sprite:** runtime frames are pushed onto a `Sprite` in `Sprite.UpdateToAnimationFrame` (called from `UpdateToCurrentAnimationFrame` / `AnimateSelf`). That method is the single hook where per-frame texture/coords/flip/color land on the Sprite.

## Landmines

- **Two deserialization paths, kept in sync by hand.** Desktop uses reflection-based `FileManager.XmlDeserialize<AnimationChainListSave>`. Android/iOS use a hand-written manual path (`AnimationChainListSave.DeserializeManually` / `LoadFromElement`, and `AnimationFrameSave.FromXElement` — a `switch` on element local-name). **Any new serialized element must be added to BOTH**, or it loads on desktop and silently vanishes on mobile.
- **`ShouldSerializeXxx()` controls XML output.** Save-class fields use `ShouldSerializeXxx()` methods so defaults/nulls are omitted from the `.achx`. New optional fields follow this pattern to stay backward-compatible (old files just lack the element).
- **Coordinate + time units differ from runtime.** `AnimationChainListSave.CoordinateType` is UV *or* Pixel — `ToAnimationFrame` converts Pixel→UV by dividing by texture width/height. `TimeMeasurementUnit` (seconds vs. milliseconds) makes `ToAnimationChain` divide `FrameLength` by 1000. The runtime is always UV + seconds.

## Per-frame color (signpost)

Frames carry optional nullable tint: `Red/Green/Blue/Alpha` and a color operation. The `.achx`/Save side stores 0–255 ints and an editor-flavored op enum; the runtime side stores 0–1 floats and `FlatRedBall.Graphics.ColorOperation`, with mapping done in `AnimationFrameSave.ToAnimationFrame`. Applied to the Sprite in `Sprite.UpdateToAnimationFrame` (`ApplyAnimationFrameColor`). Read the source there for the exact channels, identity-on-null rules, and op mapping.

## Key files

| File (`Engines/FlatRedBallXNA/FlatRedBall/`) | Purpose |
|---|---|
| `Content/AnimationChain/AnimationChainListSave.cs` | `.achx` root; `FromFile`, `ToAnimationChainList`, manual load |
| `Content/AnimationChain/AnimationChainSave.cs` | one chain; `ToAnimationChain`, `FromXElement` |
| `Content/AnimationChain/AnimationFrameSave.cs` | one frame; `ToAnimationFrame`, `FromXElement`, color map |
| `Graphics/Animation/AnimationFrame.cs` | runtime frame |
| `Sprite.cs` | `UpdateToAnimationFrame` — where a frame is applied to a Sprite |
