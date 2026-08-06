---
name: gum-codegen
description: Use when working on Glue's Gum code generation — adding/removing/version-gating generated properties on Gum standard runtime types (NineSlice, Text, Container, Sprite, etc.), or debugging "why is/isn't this Gum variable being generated."
version: 1.0.0
---

# Gum Code Generation in Glue

Glue does **not** rely on Gum's own runtime code generation for the standard element runtimes. Instead, the Gum plugin regenerates these classes itself from the Gum project data. The upside is tight control over what gets emitted; the downside is that whenever Gum adds, removes, or renames a variable on a standard type, Glue's generator has to be updated to match — otherwise the new variable is silently emitted (causing build/runtime errors on older projects) or silently omitted (causing missing properties on newer projects).

This skill is about the omission side: how Glue decides which Gum variables to skip when generating a standard runtime, and how to gate that decision on a `GluxVersions` value. For the version-numbering rules themselves, see the `gluj-versions` skill.

## There are THREE independent decisions, not one

Generating a standard runtime like `NineSliceRuntime.Generated.cs` involves three independent code paths. **All three must agree on whether a member exists in a given version**, and each disagreement has its own compiler error:

1. **Property generation** — emits the `public T Foo { get; set; }` (or backing-field) property on the runtime class. Driven by `StandardsCodeGenerator` and per-type generators (`NineSliceCodeGenerator`, etc.).
2. **State generation** — emits the `case VariableState.Default: Foo = …;` assignments inside the state-switch. Driven by `StateCodeGenerator`.
3. **Inheritance** — emits the `: global::Gum.Wireframe.IFooRuntime` on the class declaration, via each per-type generator's `AddAdditionalInheritance` (wired up in `StandardsCodeGenerator.GenerateStandardElementSaveCodeFor`).

Property vs. state disagreement is `CS0103: The name 'IsTilingMiddleSections' does not exist in the current context` — a state-init assignment for a property nobody generated. Inheritance vs. property disagreement is `CS0535: does not implement interface member` (issue #1979).

Pipelines 1 and 2 are driven by the project's variables; pipeline 3 is driven by `GluxVersions` alone, so it is the one that most easily drifts. The `Gum.Wireframe.I*Runtime` interfaces it names are hand-written in the sibling Gum repo under `#if FRB` (`Gum/Wireframe/CustomSetPropertyOnRenderable.cs`) and mirror what Glue generates; they grow as Gum routes more of its property dispatch through the runtime instead of the renderable. Glue holds no reference to GumCore, so nothing in Glue can read one — `NineSliceCodeGenerator.InterfaceMemberNames` restates the contract by hand, and `GumGeneratedCodeCompilesTests` is what detects drift. `ContainerCodeGenerator`/`PolygonCodeGenerator` avoid the problem differently, by emitting their interface members from `GenerateAdditionalMethods` rather than relying on the project's variables at all.

When you add a version gate, **update every pipeline that touches the member**. Searching for an existing skipped variable name (e.g. `"IgnoredByParentSize"`, `"StackSpacing"`) is the fastest way to spot every place that needs a parallel change — they typically appear in both `StandardsCodeGenerator.RefreshVariableNamesToSkipForProperties` and `StateCodeGenerator.RefreshVariableNamesToSkipBasedOnGlueVersion`.

## Glue never back-fills a loaded project's standard elements (landmine)

Codegen emits from the variables in the project's own `.gutx`, and **Glue never reconciles that against Gum's canonical schema**. Gum's tool does (`GumProjectSaveExtensionMethods.Initialize` back-fills each standard element from `StandardElementsManager`, gated by `VariableSave.MinimumGumxVersion`); Glue calls `GumProjectSave.Load` and never that. What it *does* call — `FileReferenceTracker.InitializeElements` — passes each element its **own** default state, which resolves variable types but back-fills nothing.

Consequences worth knowing before debugging "why isn't this variable generated":

- A `.gutx` written before Gum added a variable never gains it, at any Glue version. Every checked-in sample is in this state.
- Bumping `GluxVersions` alone will not make a variable appear — if the project's `.gutx` lacks it, no gate change helps.
- A fixture built from `StandardElementsManager.Self.GetDefaultStateFor(name)` is a *current* editor's output, so it cannot reproduce this. Load a real sample's `.gumx` and call `Initialize(element.DefaultState)` per element to match production — see `GumGeneratedCodeCompilesTests.LegacyGutxStandardElementRuntimes_ShouldCompileAgainstTheRealEngine`.
- `ProjectLoader.LoadProject` bumps the `.gluj`'s `FileVersion` to `LatestVersion` on load. So the live combination is always **current `GluxVersions` + whatever the `.gutx` happens to contain**, which is exactly what produced #1979.

## The compiler can never catch a runtime mismatch (landmine)

Glue has **no compile-time knowledge of the runtime types it generates against**. `mStandardElementToQualifiedTypes` holds fully-qualified type names as plain *strings*; `RenderingLibrary.Graphics.Text` is not a referenced assembly in any Glue project, and it is not among GumPlugin's embedded `LibraryFiles` resources either (`LineRectangle.cs` is embedded, `Text.cs` is not). So emitting `ContainedText.DropshadowBlur` for a member that doesn't exist compiles Glue perfectly and only fails in the *user's* game project, as CS1061.

This is why the bug class recurs: Rectangle/Circle's fill/stroke family (#1907), the Arc/ColoredCircle gradient CS0266, and Text's dropshadow-channel + `LocalizeText` family were each found by a user's build breaking, not by CI.

`GlueUnitTests/GumPlugin/GumRuntimeMemberContractTests.cs` is the guard: it builds the engine's Forms solution, reflects over the real `GumCore`/`SkiaInGum` assemblies, and asserts every `ContainedXxx.Member` the generator emits exists on the mapped runtime type. It's driven off `StandardsCodeGenerator.StandardElementToQualifiedTypes`, so a standard element added to that map is covered automatically. When Gum extends a standard element's schema, that test — not the compiler — is what tells you whether FRB's runtime can back it.

Note the fix is per-member, not per-type: `Text` legitimately backs `HasDropshadow`/`DropshadowOffsetX`/`DropshadowOffsetY` (it has a single `DropshadowColor`) while having no per-channel color, no blur, and no `LocalizeText`. Skip only what's actually unbacked.

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

## Live GumxVersions gating — a different axis than the GluxVersions skip-lists above

Everything above gates on `GluxVersions` (Glue's own file format) at skip-list-refresh time. A standard element's variable family — and even which qualified runtime *type* it maps to — can instead need gating on Gum's own project version (`GumxVersions`, e.g. `ShapeVariableExpansion`), read live off `Gum.Managers.ObjectFinder.Self.GumProjectSave.Version` at generation time rather than cached in a skip-list refresh. This matters when one generator instance/process must serve whatever gumx version happens to be loaded (old and new projects both use the same Glue build), so the decision can't be baked in once. See `StandardsCodeGenerator.IsRectangleFillStrokeSupported`/`GetQualifiedTypeFor` for the pattern: a live bool checked both when choosing the contained type (`mStandardElementToQualifiedTypes` lookup vs. an override) and when deciding per-property generation, instead of a static skip list.

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
3. Update **every** pipeline that touches it:
   - Property pipeline: `ExcludeIfVersionLessThan(...)` in `StandardsCodeGenerator.RefreshVariableNamesToSkipForProperties` (global) or the `NineSliceCodeGenerator`-style `HasFeature` bool + conditional `Add` (type-specific).
   - State pipeline: an `if/else` `Include` / `Skip` block in `StateCodeGenerator.RefreshVariableNamesToSkipBasedOnGlueVersion`.
   - Inheritance: only if the member belongs to a `Gum.Wireframe.I*Runtime` interface — gate the members on the *same* bool as `AddAdditionalInheritance`, never a separate one.
4. The gate's polarity is **"skip when below version"**, i.e. older projects don't see the new variable. The new variable becomes visible at and above the gating version.

A pre-flight check that's saved time more than once: pick one already-gated variable (`IgnoredByParentSize` is a good one) and grep for it. Every place it appears is a place your new variable probably also needs to appear.
