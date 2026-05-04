---
name: gum-codegen
description: Use when working on Glue's Gum code generation — adding/removing/version-gating generated properties on Gum standard runtime types (NineSlice, Text, Container, Sprite, etc.), or debugging "why is/isn't this Gum variable being generated."
version: 1.0.0
---

# Gum Code Generation in Glue

Glue does **not** rely on Gum's own runtime code generation for the standard element runtimes. Instead, the Gum plugin regenerates these classes itself from the Gum project data. The upside is tight control over what gets emitted; the downside is that whenever Gum adds, removes, or renames a variable on a standard type, Glue's generator has to be updated to match — otherwise the new variable is silently emitted (causing build/runtime errors on older projects) or silently omitted (causing missing properties on newer projects).

This skill is about the omission side: how Glue decides which Gum variables to skip when generating a standard runtime, and how to gate that decision on a `GluxVersions` value. For the version-numbering rules themselves, see the `gluj-versions` skill.

## There are TWO skip pipelines, not one

Generating a standard runtime like `NineSliceRuntime.Generated.cs` involves two independent code paths, each with its own skip lists. **Both must agree on whether a variable exists in a given version.** If they disagree, you get build errors like:

> CS0103: The name 'IsTilingMiddleSections' does not exist in the current context

— because one pipeline emitted a state-init assignment for a property the other pipeline didn't generate (or vice versa).

The two pipelines are:

1. **Property generation** — emits the `public T Foo { get; set; }` (or backing-field) property on the runtime class. Driven by `StandardsCodeGenerator` and per-type generators (`NineSliceCodeGenerator`, etc.).
2. **State generation** — emits the `case VariableState.Default: Foo = …;` assignments inside the state-switch. Driven by `StateCodeGenerator`.

When you add a version gate, **always update both**. Searching for an existing skipped variable name (e.g. `"IgnoredByParentSize"`, `"StackSpacing"`) is the fastest way to spot every place that needs a parallel change — they typically appear in both `StandardsCodeGenerator.RefreshVariableNamesToSkipForProperties` and `StateCodeGenerator.RefreshVariableNamesToSkipBasedOnGlueVersion`.

## Where it lives

Property pipeline:
- Central orchestrator: `FRBDK/Glue/GumPlugin/GumPlugin/CodeGeneration/StandardsCodeGenerator.cs`
- Per-type contributors: `NineSliceCodeGenerator.cs`, `TextCodeGenerator.cs`, `ContainerCodeGenerator.cs`, etc. (same folder)
- Wired up in `MainGumPlugin.cs` (each per-type generator is instantiated and passed into `StandardsCodeGenerator`).

State pipeline:
- `FRBDK/Glue/GumPlugin/GumPlugin/CodeGeneration/StateCodeGenerator.cs`
- Two relevant entry points: `RefreshVariablesToSkipForStates()` (unconditional skips and per-type contributions) and `RefreshVariableNamesToSkipBasedOnGlueVersion()` (version-gated skips, the right place for a `GluxVersions`-keyed gate).

## The two skip lists

`StandardsCodeGenerator` keeps two collections that drive omission, both cleared and rebuilt every time `RefreshVariableNamesToSkipForProperties()` runs:

1. `mVariableNamesToSkipForProperties` — `List<string>`. Applies to **every** standard element. Use for variables that should never be generated regardless of type (`X`, `Y`, `Width`, `Visible`, etc., already handled by `GraphicalUiElement`).
2. `_typedVariableNamesToSkipForProperties` — `Dictionary<string, List<string>>` keyed by standard element name (e.g. `"NineSlice"`). Use for variables that only exist on a specific type and should be skipped only there.

Both lists are consumed by `GetIfShouldGenerateProperty(variable, standardElementSave)`, which is called for every variable on the default state during generation. Anything in either list short-circuits to `false`.

## Version-gating an omission

There are two patterns depending on whether the variable is type-specific.

**Global (any standard element):** use the local `ExcludeIfVersionLessThan` helper inside `RefreshVariableNamesToSkipForProperties`:

```csharp
ExcludeIfVersionLessThan("CustomFrameTextureCoordinateWidth", GluxVersions.GumUsesSystemTypes);
```

This adds the name to `mVariableNamesToSkipForProperties` only when the loaded project is below the gating version.

**Type-specific:** put the gate on the per-type generator as a `bool` property, then conditionally add to its contribution. `NineSliceCodeGenerator` is the canonical small example:

```csharp
bool HasNineSliceAnimate =>
    _glueState.CurrentGlueProject?.FileVersion >= (int)GluxVersions.GumNineSliceHasAnimate ||
    _glueState.CurrentMainProject?.IsFrbSourceLinked() == true;

internal void AddTypeSpecificVariableNamesToSkipForProperties(
    Dictionary<string, List<string>> typedVariableNamesToSkipForProperties)
{
    var variablesToIgnore = new List<string>();
    typedVariableNamesToSkipForProperties.Add("NineSlice", variablesToIgnore);

    if (!HasNineSliceAnimate)
    {
        variablesToIgnore.Add("Animate");
    }
}
```

Two things to note in that pattern:

- `IsFrbSourceLinked()` plays the same role as `REFERENCES_FRB_SOURCE` in embedded code files: if the user references FRB as source, treat the feature as available regardless of `FileVersion`. Always include this arm for type-specific gates.
- The list is registered in the dictionary unconditionally (with the type name as key). Only the *contents* are conditional. This is so `GetIfShouldGenerateProperty` can do a single dictionary lookup per type.

## State-pipeline gate

`StateCodeGenerator.RefreshVariableNamesToSkipBasedOnGlueVersion` uses a paired `Include` / `Skip` helper pattern. To gate a new variable, add a block alongside the existing ones:

```csharp
if (version >= (int)GluxVersions.NineSliceHasTilingMiddleSections)
{
    Include("IsTilingMiddleSections");
}
else
{
    Skip("IsTilingMiddleSections");
}
```

For a variable that only exists on a single standard type (like `IsTilingMiddleSections` on `NineSlice`), a global skip is fine — older projects don't have that type with that variable, so a global gate is harmless and matches existing precedent (`StackSpacing`, `IgnoredByParentSize`, `IsBold`, etc., are all gated globally here).

There's also a per-type variant analogous to the property pipeline (`AddTypeSpecificVariableNamesToSkipForStates` on per-type generators, dictionary-keyed by element name). Use it only when a variable name might collide across types and the gate must apply to one type.

## When you're adding a new gate

1. Decide if the variable is global or type-specific.
2. Add a `GluxVersions` entry per the `gluj-versions` skill (including `LatestVersion`, `SyntaxVersionAttribute`, docs).
3. Update **both** pipelines:
   - Property pipeline: `ExcludeIfVersionLessThan(...)` in `StandardsCodeGenerator.RefreshVariableNamesToSkipForProperties` (global) or the `NineSliceCodeGenerator`-style `HasFeature` bool + conditional `Add` (type-specific).
   - State pipeline: an `if/else` `Include` / `Skip` block in `StateCodeGenerator.RefreshVariableNamesToSkipBasedOnGlueVersion`.
4. The gate's polarity is **"skip when below version"**, i.e. older projects don't see the new variable. The new variable becomes visible at and above the gating version.

A pre-flight check that's saved time more than once: pick one already-gated variable (`IgnoredByParentSize` is a good one) and grep for it. Every place it appears is a place your new variable probably also needs to appear.
