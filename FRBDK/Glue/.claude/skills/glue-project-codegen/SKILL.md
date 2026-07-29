---
name: glue-project-codegen
description: Glue's .gluj/.glsj/.glej project JSON format and how it drives Screen/Entity .Generated.cs code generation. Triggers: GlueProjectSave, ScreenSave, EntitySave, NamedObjectSave, CustomVariable, CodeWriter, ElementComponentCodeGenerator, .gluj, .glsj, .glej, .glux.
version: 1.0.0
---

# Glue Project JSON → Generated Code

## File formats

| Extension | Contents | Format |
|---|---|---|
| `.glux` | Legacy single-file project (whole project inline) | XML (`System.Xml.Serialization`) |
| `.gluj` | Current root project file (Screens/Entities referenced by name once split) | JSON (Newtonsoft.Json) |
| `.glsj` | One file per Screen | JSON |
| `.glej` | One file per Entity | JSON |
| `*.generated.glux`/`*.generated.gluj` | Plugin-owned partial fragments merged into the main file at load | XML/JSON |

`.glux`→`.gluj` and the later per-element split into `.glsj`/`.glej` are both `GluxVersions`-gated migrations — see the `gluj-versions` skill for the version enum and gating workflow; don't duplicate that history here.

Load entry point: `GlueProjectSaveExtensions.Load` in `FRBDK/Glue/Glue/SaveClasses/GlueProjectSaveExtensions.cs` — prefers `.gluj` over `.glux` if both exist, falls back to `.glux` if no `.gluj` (mid-migration tolerance), then loads per-element `.glsj`/`.glej` files individually if `FileVersion >= SeparateJsonFilesForElements`. Save entry point: `SaveMainAndElementsToFile` (same file) → `RemoveAndSaveElements` for per-element files. Both driven from `FRBDK/Glue/Glue/IO/ProjectLoader.cs` and `GluxCommands.SaveProjectAndElements*`.

Schema is a plain POCO graph, not a `.schema.json` — the canonical source is `FRBDK/Glue/GlueCommon/SaveClasses/` (`GlueProjectSave.cs`, `ScreenSave.cs`, `EntitySave.cs`, `GlueElement.cs`, `NamedObjectSave.cs`, `CustomVariable.cs`, `StateSave.cs`, `ReferencedFileSave.cs`, `PropertySave.cs`, `VariableDefinition.cs`). A **second, trimmed copy** of `GlueProjectSave`/`GluxVersions` lives at `FRBDK/Glue/GameCommunicationPlugin/GlueControl/Embedded/Models/GlueProjectSave.cs`, compiled as source into the user's game for live in-editor editing — it's frozen at an old version and drifts from the editor's own enum; don't assume version parity between the two when touching live-edit/hot-reload code.

## Serialization gotchas (landmines)

- **`PropertySave.Value` boxes as `long`/`double`, not `int`/`float`.** Newtonsoft deserializes bare JSON numbers that way. Always read through `PropertySaveListExtensions.GetValue<T>` (`PropertySave.cs`), which special-cases the conversions — reading `.Value` directly and casting will throw or silently misbehave.
- **Enums serialize as raw ints** — no `StringEnumConverter` anywhere in the save classes. Hand-editing a `.gluj` requires knowing the enum's underlying int value.
- **`NullValueHandling`/`DefaultValueHandling` differ between the main file and per-element files.** Main `.gluj`: `NullValueHandling.Ignore` + `DefaultValueHandling.IgnoreAndPopulate`. Per-element `.glsj`/`.glej`: `DefaultValueHandling.Ignore` only. Both live in `GlueProjectSaveExtensions.cs`.
- **New properties on save classes must default to the type's own default value**, never a custom default — a comment at `GlueProjectSave.cs` states this explicitly, since `IgnoreAndPopulate` means the JSON omits anything at default and repopulates it on load. Giving a property a non-standard default silently breaks backward compatibility for every existing project file.
- **Per-element load tolerates a corrupt file**: a null/failed `ScreenSave`/`EntitySave` deserialize is skipped rather than aborting the whole project load, so a single bad `.glsj`/`.glej` merge doesn't brick the project.
- **`GlueSettingsSave` (user app settings, not project data) intentionally never finished migrating to JSON** — still XML, with a commented-out `JsonConvert` call and a note that build tool associations broke under JSON (suspected `TypeConverter` interaction). Don't "fix" this without digging into that history first.
- Saves call `FileWatchManager.IgnoreNextChangeOnFile(...)` before writing to suppress self-triggered reload — general mechanism covered by the `glue-file-watch` skill.

## JSON → generated code pipeline

Entry points: `CodeWriter.GenerateCode(GlueElement element)` (`FRBDK/Glue/Glue/CodeGeneration/CodeWriter.cs`) builds one Screen/Entity's `<ElementName>.Generated.cs`; `CodeGeneratorIElement.cs` wraps it (`GenerateElementAndDerivedCode` also regenerates inheriting elements); `GenerateCodeCommands` (`Glue/Plugins/ExportedImplementations/CommandInterfaces/GenerateCodeCommands.cs`) is the public command surface, including `GenerateAllCode` for full-project regen.

Generation is a **plugin pipeline**, not one big method: each contributor subclasses `ElementComponentCodeGenerator` (`CodeGeneration/ElementComponentCodeGenerator.cs`), overriding phase methods (`GenerateFields`, `GenerateConstructor`, `GenerateInitialize`, `GenerateAddToManagers`, `GenerateActivity`, `GenerateDestroy`, `GenerateEvent`, `HandlesVariable`, …). `CodeWriter`'s static ctor lists the built-ins in order (fields → static ctor → ctor → Initialize → AddToManagers → Activity → Destroy → additional methods): `ErrorCheckingCodeGenerator, ScrollableListCodeGenerator, StateCodeGenerator, FactoryElementGeneratorEarly, FactoryElementCodeGenerator, ReferencedFileSaveCodeGenerator, NamedObjectSaveCodeGenerator, CustomVariableCodeGenerator, EventCodeGenerator, PooledCodeGenerator, IVisibleCodeGenerator, IWindowCodeGenerator, ITiledTileMetadataCodeGenerator, PauseCodeGenerator, LoadingScreenCodeGenerator`. External plugins (Gum, TopDown, Platformer, RedGrin, …) contribute their own generators via `ICodeGeneratorPlugin` (`Glue/Plugins/Interfaces/ICodeGeneratorPlugin.cs`) — currently only its `GenerateActivity`/`GenerateAdditionalMethods` hooks are wired up (there's a `//TODO: Add more generation types here` marking the gap).

The two save-class types that map most directly to generated members:
- **`NamedObjectSave`** (instance placed in a Screen/Entity) → `NamedObjectSaveCodeGenerator.cs`: field + constructor/Initialize instantiation + `AddToManagers` wiring. `CodeGenerationType` (Full vs OnlyContainedObjects vs Nothing) controls whether a field is generated fresh or the object is assumed already declared by an inherited base — get this wrong in a custom generator and you get duplicate-field build errors.
- **`CustomVariable`** (exposed/tunneled variable) → `CustomVariableCodeGenerator.cs`: either a new C# property or a tunneled property forwarding to a `NamedObjectSave` member. Base-class variables are *not* regenerated in derived elements unless tunneling, shared, or creating an event — overriding that precedence incorrectly silently drops the override.

Regeneration is **synchronous-on-edit**, not file-watch or debounce driven: nearly every mutating Glue command (add/remove/rename object, variable, screen, entity, event) calls `GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element)` directly, queued onto `TaskManager.Self` to run off the UI thread in order. `GenerateAllCode` additionally fires on project load and via an explicit "Regenerate All Code" command.

Other gotchas:
- `EntireClassCodeGenerator` is `[Obsolete]` — use `FullFileCodeGenerator` (`Glue/Plugins/CodeGenerators/FullFileCodeGenerator.cs`) for new standalone-file generators.
- Generators never emit `using` statements — deliberate, since multiple plugins' generators could pick conflicting types for the same short name. Use fully-qualified names in generated code.
- The hand-written half of a Screen/Entity (`CustomInitialize`, `CustomActivity`, `CustomDestroy`, `CustomLoadStaticContent`) lives in a sibling non-`.Generated.cs` partial seeded from `CodeWriter.ScreenTemplateCode`/`EntityTemplateCode` — the `.Generated.cs` partial calls into these at fixed points.

## Related skills

- `gluj-versions` — the `GluxVersions` enum, `FileVersion` gating, and the version-bump checklist. This skill assumes that context; don't restate it here.
- `gum-codegen` — Gum standard-runtime (NineSlice/Text/Container/…) property and state codegen specifically. Separate pipeline from the general `ElementComponentCodeGenerator` list above.
- `glue-file-watch` — the general external-change-detection and self-save-suppression mechanism referenced above.
