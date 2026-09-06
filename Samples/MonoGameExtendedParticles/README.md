# MonoGame.Extended Particles in FlatRedBall

A code-only FlatRedBall project that renders a [MonoGame.Extended](https://www.monogameextended.net/)
`ParticleEffect` through an [IDrawableBatch](https://docs.flatredball.com/flatredball/api/flatredball/graphics/drawablebatch),
so particles sort and move with FlatRedBall's camera like any other FlatRedBall object.

FlatRedBall has its own particle system built around [Emitter](https://docs.flatredball.com/flatredball/api/flatredball/graphics/particle/emitter)
and `.emix` files, and for most games that is the simpler choice. Use this integration when you
want MonoGame.Extended's authoring tools or its modifier and interpolator set.

## Running it

```
dotnet run --project MonoGameExtendedParticles/MonoGameExtendedParticles.csproj
```

- Arrow keys pan the camera
- Z and X zoom out and in
- Left click fires a burst at the cursor

The blue rectangle marks the world origin. The fountain should stay locked to it through
every pan and zoom - that is the part an integration like this gets wrong.

## What to read

`ParticleEffectDrawableBatch.cs` is the whole integration and the only file worth copying into
another project. `Screens/GameScreen.cs` builds two effects in code and drives the camera.

## Things that bite

**Coordinate space.** MonoGame.Extended simulates and draws particles in SpriteBatch
coordinates, where +Y points down. FlatRedBall's world is +Y up. `ParticleEffectDrawableBatch`
keeps the simulation in MonoGame.Extended's own space and converts FlatRedBall world
coordinates when triggering, rather than flipping Y in the transform matrix - a matrix flip
also mirrors every particle texture and reverses particle rotation. The practical consequence
is that a `LinearGravityModifier` pointing at `Vector2.UnitY` falls down the screen, the way
it reads.

**`Update` runs during Draw.** FlatRedBall calls `IDrawableBatch.Update` inside its draw phase,
so that is where the particle simulation is advanced, and it must not change render targets.

**Render state is not preserved.** Every `Draw` call sets its own blend and sampler state.
Note also that MonoGame.Extended premultiplies particle color by opacity only when the device
blend state is `AlphaBlend`, and writes opacity into the alpha channel otherwise, so blend
state changes how an effect fades and not just how it composites.

**`AutoTrigger` defaults to true.** `ParticleEffect`'s constructor turns it on, so an effect
meant to fire only on demand emits once a second on its own until you set it to false.

**MonoGame versions.** MonoGame.Extended 6.1.1 is compiled against MonoGame 3.8.5 and binds to
that exact assembly version, while the engine project references 3.8.4.1. This sample pins
`MonoGame.Framework.DesktopGL` 3.8.5.1 so NuGet unifies on the higher version; FlatRedBall runs
on it fine. Pinning 3.8.4.1 instead builds cleanly and then throws `FileNotFoundException` for
`MonoGame.Framework 3.8.5.0` the first time particle code runs.

**Name collisions.** FlatRedBall, MonoGame, and MonoGame.Extended each define a `Sprite`, and
FlatRedBall and MonoGame each define a `Mouse`. `GameScreen.cs` aliases rather than importing
`MonoGame.Extended.Graphics` wholesale.

## Using an effect authored in Ember

The sample builds its effects in code so it needs no content pipeline. To use an effect
authored in [Ember](https://www.monogameextended.net/docs/tools/ember/) instead, swap the
construction for `ParticleEffect.FromFile(path, contentManager)` and hand the result to
`ParticleEffectDrawableBatch`. Nothing in the drawable batch changes.
