---
name: color-operations
description: FlatRedBall ColorOperation enum (Add, Modulate, Color, etc.) shared across visual object types. Triggers: ColorOperation, mColorOperation, Sprite.ColorOperation, RenderableIpso, RenderableSkiaObject, AnimationFrame.ColorOperation, UpdateVertexColorsAccordingToAlpha.
---

## Where it lives

`ColorOperation` enum: `Engines/FlatRedBallXNA/FlatRedBall/Graphics/GraphicalEmumerations.cs`.

It's not Sprite-specific — consumers include:

| Type | File | Notes |
|---|---|---|
| `Sprite` | `Engines/FlatRedBallXNA/FlatRedBall/Sprite.cs` | Full support, all enum values; setter calls `UpdateVertexColorsAccordingToAlpha` |
| `AnimationFrame` | `Engines/FlatRedBallXNA/FlatRedBall/Graphics/Animation/AnimationFrame.cs` | Nullable per-frame override, applied in `Sprite.UpdateToAnimationFrame` |
| `RenderableSkiaObject` (Gum/SkiaSharp text, SVG) | `Engines/SkiaGum/Renderables/RenderableSkiaObject.cs` | **Hardcoded to `Modulate`, read-only** — does not respect Add/Subtract/etc. |
| `Renderer` global state | `Engines/FlatRedBallXNA/FlatRedBall/Graphics/Renderer.cs` | Static `ColorOperation` property drives shader technique selection via `_effectManager.GetVertexColorTechniqueFromColorOperation` |

## Gotchas

- **Skia-rendered objects (Text/Gum via `RenderableSkiaObject`) silently ignore non-Modulate operations** — the `IRenderableIpso.ColorOperation` getter always returns `Modulate`, there's no setter. Don't expect `Add`/`Subtract`/etc. to work outside the XNA `Sprite` render path.
- **Debug-only platform restriction**: on iOS/Android/WinRT, setting `Sprite.ColorOperation` to `Add`, `Subtract`, `InterpolateColor`, `InverseTexture`, `Modulate2X`, or `Modulate4X` throws in DEBUG builds (`Sprite.cs` ~line 410) — these ops aren't guaranteed cross-platform.
- **`"AddSigned"` is not `ColorOperation.AddSubtract`.** It's a separate legacy string op-code handled only in `GraphicalEnumerations.SetColors` (`GraphicalEmumerations.cs` ~line 293), which biases `desiredRed/Green/Blue` by `-127.5` and remaps to plain `"Add"` — no enum value, no `VertexColorPacker` involvement.
- `ColorOperation.Texture` with a null `Texture` behaves like `ColorOperation.Color` (see `Renderer.cs` ~line 3349, `Sprite.cs` ~line 256) — a common "why does my untextured sprite still show color" trip-up.
- **`Sprite.Red/Green/Blue` clamp to `[-1, 1]`, not `[0, 1]`** (`Sprite.cs` ~line 301-304), and nothing downstream re-clamps before GPU upload. Packing (now `VertexColorPacker.Pack`, called from `SpriteManager.cs` ~line 2146) casts the float channel to `uint` via `(uint)(255 * value)`. **A negative float→uint cast here is undefined behavior per ECMA-335** (out-of-range float-to-unsigned conversion is unspecified) and is genuinely runtime-dependent, not just "clamps to 0": on .NET 8/x64 (this repo's actual `net8.0` target) it wraps to byte `129`; on .NET 10 the same code gives `0`. Either way it is NOT a clean subtraction — never rely on negative `Red`/`Green`/`Blue` under `ColorOperation.Add` to produce predictable output; use `ColorOperation.AddSubtract` instead (bias/scale-encodes the signed value so it survives the UNORM byte round-trip predictably).
