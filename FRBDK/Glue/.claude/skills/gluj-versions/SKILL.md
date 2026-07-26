---
name: gluj-versions
description: Use when adding, modifying, or reasoning about Gluj/Glux file format versions, the GluxVersions enum, FileVersion checks, or anything that gates behavior on the version of a loaded Glue project.
version: 1.0.0
---

# Gluj Versions

Glue projects (`.gluj`/`.glux`) are versioned so that the editor and runtime can load older projects while still adding new features. Versions are tracked by the `GluxVersions` enum and `LatestVersion` constant in:

`FRBDK/Glue/GlueCommon/SaveClasses/GlueProjectSave.cs`

For background on what each version means and the historical context, see the official docs, published from `https://docs.flatredball.com/flatredball/glue-reference/glujglux`. That docs site is a GitBook synced from a **separate sibling repo checked out locally**, `C:\Users\vchel\Documents\GitHub\FlatRedBallDocs` (relative to this repo, same convention as the Gum sibling repo) — plain markdown, matching the site's URL structure. The glujglux page is `glue-reference/glujglux.md` in that repo. Edit the file directly rather than treating the docs as remote-only/unreachable.

## When adding a new version

Adding a new entry to `GluxVersions` requires updating several places in lockstep. Miss one and projects will silently load with the wrong feature set.

1. Add the new enum entry at the bottom of `GluxVersions` with a dated comment describing what it represents.
2. Update `LatestVersion` to point at the new entry.
3. Update the `SyntaxVersionAttribute` on `FlatRedBallServices` so the runtime advertises the matching version.
4. Update the docs (the local `FlatRedBallDocs` repo file above), adding a new `### Version N - <title>` section matching the existing entries' style.

Multiple enum entries can share the same integer value when several features land together — this is intentional and already common in the enum.

## Retroactively gating a missed breaking change

Sometimes a breaking change ships without being gated behind a version, and the gap is only noticed after a later version has already been cut. In that case, **do not** invent a new version number. Instead, add a new enum entry at the **same integer value** as the most recent version that already shipped after the change, with a dated comment explaining the retroactive add. Upgrading to that version then pulls in both changes together.

Example: version 66 (`GumHasGueVirtualIsPointInside`, Jan 29 2026) was cut after a Jan 17 2026 change to `PositionedNode.Tag` that wasn't gated. The fix was to add `PositionedNodeHasTag = 66` next to it, not to invent a 67.

When you do this, also gate the previously-ungated code in any embedded code files that reference the new API:

- If the embedded file does not already have a `$GLUE_VERSIONS$` token at the very top, add one. That token is what the code generator replaces with `#define` lines for each enum symbol whose value is `<= FileVersion`. Without it, no `#if SymbolName` guard in the file will ever evaluate true.
- Wrap the new API usage in `#if SymbolName || REFERENCES_FRB_SOURCE`. The `REFERENCES_FRB_SOURCE` arm keeps the code live for projects that reference FRB as source rather than NuGet, where the symbol isn't defined but the API exists.

## Embedded code files: where they live and conventions

Embedded code files are templates that get copied into user projects by plugins. They live under `FRBDK/Glue/<Plugin>/EmbeddedCodeFiles/*.cs` and are registered for inclusion via `CodeItemAdderManager` (e.g. `FRBDK/Glue/TileGraphicsPlugin/TileGraphicsPlugin/Managers/CodeItemAdderManager.cs`).

For a known-good reference of the `$GLUE_VERSIONS$` + `#if SymbolName || REFERENCES_FRB_SOURCE` pattern, see `FRBDK/Glue/TileGraphicsPlugin/TileGraphicsPlugin/EmbeddedCodeFiles/CollidableListVsTileShapeCollectionRelationship.cs`.

Style: these files target older language levels because they get compiled into a wide range of user projects. **Do not** introduce C# 8 nullable reference annotations (`object?`, `string?`, etc.) just because your editor accepts them — match the surrounding sibling files, which use plain `object` / `string`.

## Caveat: gating an added optional parameter

Wrapping a *newly added optional parameter* in `#if` is the standard pattern but it has a sharp edge: the parameter visibly disappears from the method signature on projects below the gating version. Callers passing the argument by position simply don't compile (loud, easy to fix), but callers passing it by name (`FillFromPredicate(..., tagForAddedNodes: x)`) get a confusing "no such parameter" error rather than a version-mismatch hint.

This is the price of supporting projects that don't pull FRB as source — every embedded file in the codebase does it the same way. Mention this tradeoff if a user is on the fence about gating vs. raising the minimum version.

## When gating behavior on a version

Compare `FileVersion` against a named `GluxVersions` member, never a magic number:

```csharp
if (this.FileVersion < (int)GluxVersions.SomeFeature) { ... }
```

This keeps the intent readable and survives renumbering.
