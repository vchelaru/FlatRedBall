---
name: shapes-collision-attachment
description: How FlatRedBall shapes attach to entities vs. participate in collision — PositionedObject.Children vs ICollidable.Collision. Triggers: SetCollisionFromAnimation, ICollidable, ShapeCollection, AttachTo, animation-driven shapes, bullet-origin/marker shapes.
---

# Shapes, Collision, and Attachment

Two separate things get conflated here. Don't assume one implies the other.

1. **Attachment.** A shape (Circle, AxisAlignedRectangle, Polygon, etc.) is a `PositionedObject`. `AttachTo` makes it move with a parent, same as any other child (`PositionedObject.Children`, `PositionedObject.cs:288`). This works on any entity, `ICollidable` or not.
2. **Collision membership.** `ICollidable` (`Math/Geometry/ICollidable.cs:14`) adds one more thing: a `Collision` `ShapeCollection`. A shape only affects collision detection if it's also added to `.Collision` — being attached doesn't do that on its own.

`Sprite.SetCollisionFromAnimation` (`Sprite.cs:2044`) requires `ICollidable` because it writes into `.Collision`, not because reading shapes off an animation frame needs it. Custom code can read `CurrentFrame.ShapeCollectionSave` and use it however it wants on any entity — see `Enemy.GetNewBulletPosition()` in CrankyChibiCthulhu for a non-collision example (a `BulletOrigin` circle used to position bullet spawns).

**Landmine:** `createMissingShapes: true` on `SetCollisionFromAnimation` adds *any* unmatched named shape in the current frame to `.Collision`, including one only meant as a position marker. There's currently no way to sync a shape as a child from animation without risking it becoming a live collision shape if that flag is on.

Related: `achx-format` skill for how shapes are stored per animation frame.
