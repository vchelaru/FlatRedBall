---
name: test-harnesses
description: Map of FRB1's existing test harnesses (fake game socket, gold project load+build, real game launch, embedded-code tests) — check here before saying a scenario can't be tested. Triggers: TDD, "how would we test", end-to-end, LiveGameProcess, GoldProject, FakeGameSide, BuildSmoke, LiveGame.
---

# Test Harnesses

Before calling a scenario untestable, or proposing a new harness, check this table and then `ls` the
`TestSupport` folders. Most "we'd need a harness that..." ideas already exist. See [[glue-live-edit]]
for the live-edit system these harnesses exercise.

| Need | Harness | Category / CI |
|---|---|---|
| Glue-side socket behavior with no game | `FRBDK\Glue\Tests\GlueUnitTests\GameCommunicationPlugin\FakeGameSide.cs` | default, every PR |
| Real project loaded headlessly in Glue, codegen run, `dotnet build`, live-edit code embedded | `TestSupport\GoldProject.cs` (+ `PlatformerLadderGoldProject.cs` for a shared prebuilt one) | `BuildSmoke` |
| Built game launched for real and driven over the real live-edit socket | `TestSupport\LiveGameProcess.cs`, used by `Projects\LiveGameProcessTests.cs` on `Samples/EditorTest1` | `LiveGame`, every PR (`pr-tests.yml`, Mesa GL on CI) |
| Embedded (game-side) logic compiled and run in a scratch game project | `GlueControl\VariableAssignmentLogicEnumConversionTests.cs` explains the pattern | `BuildSmoke` |
| `#if` version gates in embedded code, no build | `GlueControl\EmbeddedVersionGateTests.cs` (Roslyn) | default |
| Embedded files that compile without a game, run in-process | `Tests\EngineUnitTests` links them in with `<Compile Include ... Link>` (see its `.csproj`) | engine tests |
| Glue statics/DI ready for `GlueCommands.Self`/`GlueState.Self` | `TestSupport\GlueTestBootstrap.cs` | n/a |
| Synthetic FRB2 project on disk | `TestSupport\Frb2ProjectFixture.cs` | default |

## Gotchas

- **A race doesn't need to happen for real to be tested.** Put the files and timestamps into the state the race leaves behind, then launch. This is deterministic, and the harnesses above already support it.
- **Extending a harness beats working around it.** If `LiveGameProcess.StartAsync` or `GoldProject` lacks a hook for the step you need (for example between build and launch), add the hook there instead of writing a new harness.
- **`LiveGameProcessTests`' class comment says `LiveGame` is excluded from CI. That is stale:** `pr-tests.yml` runs it on every PR. Trust the workflow files over test comments for what CI runs.
