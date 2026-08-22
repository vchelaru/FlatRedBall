---
name: platformer-physics
description: How FRB's platformer entity movement/physics works — MovementType state machine, ground/air value sets, collision-driven terrain switching. Triggers: PlatformerValues, MovementType, GroundMovement, AirMovement, AfterDoubleJump, CurrentMovement, GroundCollidedAgainst, IsOnGround, PlatformerPlugin.
---

# Platformer Physics

Platformer entities are generated code, not engine classes: **FRBDK/Glue/PlatformerPlugin** (the Glue plugin) reads `Models/PlatformerValues.cs` field values from the Glue project and emits the actual movement logic into the user's project via `CodeGenerators/EntityCodeGenerator.cs`. To change runtime platformer behavior, edit the templates in `EntityCodeGenerator.cs`; to change what values a user can configure, edit `Models/PlatformerValues.cs` + the plugin's ViewModels/Views.

## Movement value sets and state machine

A platformer entity holds up to three `PlatformerValues` sets — `GroundMovement`, `AirMovement`, `AfterDoubleJump` — plus an enum `MovementType {Ground, Air, AfterDoubleJump}`. `CurrentMovementType` setter calls `UpdateCurrentMovement()` (`EntityCodeGenerator.cs:337`) which resolves `CurrentMovement` to the matching set (falls back to `AirMovement` if `AfterDoubleJump` is unset) and applies `Gravity`→`YAcceleration`. `DetermineMovementValues()` (`EntityCodeGenerator.cs:666`) drives the Ground/Air/AfterDoubleJump transitions each frame off `mIsOnGround` and `mHasDoubleJumped`.

`mIsOnGround` is set only inside `CollideAgainst(...)` during solid-collision resolution (`EntityCodeGenerator.cs:882`), not by any standalone "am I touching the floor" check — it's a side effect of that frame's collision pass.

## Terrain-specific values (ice, water, sticky) aren't automatic

Swapping `GroundMovement`/`AirMovement` per terrain type is user code, not plugin-generated: check `GroundCollidedAgainst`/`ItemsCollidedAgainst` (populated after all of that frame's collision resolves) against named `TileShapeCollection` relationships in `CustomActivity`, then reassign e.g. `this.GroundMovement = PlatformerValuesStatic[...IceGround]`. Full pattern: `FlatRedBallDocs/tutorials/platformer-plugin/groundcollidedagainst-for-movement-values.md`.

## Value meanings

Full explanations (with tuning guidance and video examples) live in `FlatRedBallDocs/tutorials/platformer-plugin/platformer-basics/04-control-values.md`. Field reference: `Models/PlatformerValues.cs`. Key gotcha: `AccelerationTimeX`/`DecelerationTimeX` of 0 means **Immediate** movement (no easing); `JumpApplyByButtonHold` implements variable jump height by turning gravity off while held, not by delaying the jump.

## Related docs

`FlatRedBallDocs/tutorials/platformer-plugin/` has the full tutorial tree (basics, animations, ladders, moving platforms, multiplayer, etc.) — check there before re-deriving behavior from code.
