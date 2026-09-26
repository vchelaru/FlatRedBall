---
name: gum-codegen-test-rigor
description: What actually proves a Gum-codegen fix works, vs. what only looks like it does. Triggers: "tests pass but it's still broken", CS1061/CS0200/NRE surviving a green suite, verifying StandardsCodeGenerator/StateCodeGenerator/GueDerivingClassCodeGenerator changes.
---

# Gum Codegen: What Actually Proves a Fix Works

Glue's Gum codegen (`gum-codegen` skill) emits C# source text with no compile-time link to the real
runtime types it targets. That's why weak verification repeatedly looks green while real bugs ship — this
skill is the record of that pattern recurring, so the next fix doesn't restart at the bottom of this list.

## Verification strength ladder — each level misses a different bug class

From weakest to strongest, all four were tried in sequence on the same fix before it actually worked:

1. **String-matching generated source** (`generatedSource.ShouldContain/ShouldNotContain(...)`). Proves
   text presence only. Misses everything about correctness.
2. **Reflection-based member-contract checks** (`GumRuntimeMemberContractTests`). Proves a *named member*
   exists on the *contained* type, but only for patterns the check explicitly matches (e.g. a
   `ContainedXxx.Member` regex). Missed a base-class (`GraphicalUiElement.RenderableComponent`) assignment
   entirely — wrong access shape, so the pattern never matched it, so it never looked.
3. **Real compile against real assemblies** (BuildSmoke `*CreationSmokeTests` /
   `GumGeneratedCodeCompilesTests`). Proves the generated code *compiles*. Says nothing about runtime
   behavior — code that compiles clean can still NRE the instant its constructor runs.
4. **Real instantiate-and-execute.** Actually construct the generated runtime the way production code
   does, and run it. This is the only level that catches a construction-time `NullReferenceException`.
   Nothing weaker than this can.

If a fix "passed" at one level and still broke in the field, the next debugging step is always "move up
this ladder," not "add another test at the same level."

## The sharper landmine: matching the real call path, not just reaching a level

Being at the top of the ladder isn't sufficient either — a test can compile-and-run and *still* miss the
bug if it doesn't reproduce the exact parameters/state production code actually uses:

- A BuildSmoke sweep silently defaulted `GumProjectSave.Version`, so it always exercised the v2 branch and
  never touched the v3 code path it was supposedly covering.
- A fix worked when constructed with `fullInstantiation: true`, but every real screen/component instance
  is constructed with `fullInstantiation: false` (`GumRuntime.ElementSaveExtensions.CreateGueForElement`'s
  default) — a test using the wrong flag stayed green while the real path NREs.

**Before trusting any test at level 3 or 4, confirm it uses the same version flags, instantiation flags,
and call path a real generated game project actually hits — not just *some* valid input.** This is the
single most expensive-to-rediscover fact in this list; everything else follows from it.

## Two separate axes — don't conflate them when a run "looks stuck"

A test run that looks broken can be broken for two unrelated reasons: the test logic doesn't cover the
real bug (this skill), or the *environment* is jammed (orphaned `dotnet`/`testhost`/`MSBuild`/
`VBCSCompiler` processes piling up across a long session — see `glue-unit-test-bootstrap`'s standing
cleanup rule). Diagnose which one you're looking at before changing test code.

## Related
- [[gum-codegen]] — the two-pipeline skip-list mechanics this level of testing verifies.
- [[glue-unit-test-bootstrap]] — process hygiene (bootstrap init, BuildSmoke filtering, orphaned-process
  cleanup) as opposed to test *coverage*.
