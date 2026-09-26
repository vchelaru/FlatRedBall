# Glue Testing Incidents — Running Log

Not a skill. A log of what's been tried and what actually happened, so a session reset doesn't restart
the same failed attempts blind. Append newest entries at the bottom with a date. Don't delete old entries.

## 2026-08-05 — Rectangle Fill/Stroke (#1967/#1968) manual-test session, repeated "stuck tests"

- Symptom: agent reports "waiting on slow BuildSmoke test," multiple times, across a long session.
- First cleanup attempt: killed `dotnet`/`testhost`/`MSBuild`/`VBCSCompiler` only. Explicitly left
  `devenv.exe` alone (caution about killing VS with possible unsaved work) without confirming whether VS
  was actually holding the locks. **This did not fix it** — reported "proceeding fine," was wrong.
- Verified 4 min later: same `dotnet` PIDs, CPU time flat (not climbing) over 2+ minutes = genuinely
  stalled, not just slow.
- Root cause found on re-check: **3 `devenv.exe` processes still running the whole time** (114, 104, 82
  min old at time of check) — these were never killed in either cleanup pass, and are the actual lock
  holders on Glue's build output. The user closing "Glue" (the app under test) is a different process from
  closing Visual Studio (`devenv.exe`) itself — both hold locks, only closing the app was ever confirmed.
- Lesson: **"Glue is closed" (the tested app) ≠ "devenv is closed" (the IDE editing/building it).** Verify
  `devenv.exe` process count directly before trusting a test run will get a clean build, don't infer it
  from the app being closed.
- User closed all 3 `devenv.exe` instances manually. Confirmed gone via process check. Killed the
  remaining stale `dotnet`/`testhost`/`VBCSCompiler` swarm (leftover from the deadlocked run) for a true
  clean slate rather than assuming they'd self-resolve once the lock was gone. Verified 0 matching
  processes before resuming the agent.
- **Even with devenv confirmed closed and a fully clean process slate, the next run flatlined again**:
  same PIDs, CPU barely moved (18.16→18.30 CPU-sec) over 4.5 min of wall clock. So devenv/orphaned
  processes were not the only cause — something else is blocking this specific run. Not yet diagnosed;
  pinged the agent directly for its actual current status instead of guessing further. Update this entry
  once the real cause is found.
- Also confirmed separately this session: repeated stop/resume of the same background agent across many
  rounds orphans `dotnet test`/`dotnet build` process trees that never get reaped between rounds — this is
  additive across a long session (60+ stray processes observed at one point). See
  `glue-unit-test-bootstrap`'s standing pre/post-run cleanup rule for the mitigation; this log is for
  *specific incidents*, that skill is for the *timeless procedure*.

## 2026-08-05 — RESOLVED: root-caused to the BuildSmoke tests' own `Process` usage (#1969)

Measured rather than inferred, which is what finally broke the loop. The three symptoms above turned out
to be one bug plus one piece of local noise.

- **Baselines first.** Fast suite (`Category!=BuildSmoke`, `--no-build`): 25s / 280 tests — never the
  problem. Warm incremental build: ~40s. BuildSmoke, *with the MSBuild nodes already warm*: 48s / 10 tests,
  all green. So the suite was fine whenever it was measured right after another build, which is exactly why
  every attempt to reproduce on a "clean slate" kept coming back green or hanging unpredictably.
- **Reproduced deterministically by killing every MSBuild worker node first**, then running a single
  BuildSmoke test that had just taken 5.8s. It hung past 3 minutes. Process snapshot during the hang was
  conclusive: the child `dotnet build` had **already exited**, `testhost.exe` was still blocked, and 9
  `dotnet ... /nodemode:1` nodes plus `VBCSCompiler` were alive.
- **Root cause:** all seven nested-build helpers used `StandardOutput.ReadToEnd()` on a redirected child.
  `dotnet build` starts MSBuild worker nodes with `/nodeReuse:true`, and `Process`'s redirect pipes are
  inheritable, so those nodes hold the stdout write handle open after the build exits. `ReadToEnd()` returns
  on EOF, not on process exit — so it blocked until the nodes hit their 15 minute idle timeout. Only the run
  that *starts* the nodes inherits the handles, hence "hangs once, passes on immediate retry" and hence the
  earlier "even with a confirmed clean process slate it flatlined again": the clean slate was the trigger,
  not the cure. A successful run also left 20 stray nodes behind, which is the accumulation.
- The same helpers read stdout to EOF before touching stderr at all (deadlocks past ~4KB of stderr) and had
  no timeout, so any of the above wedged the run instead of failing it.
- **Fixed** by routing all seven through `GlueUnitTests/TestSupport/NestedDotnetCli.cs`. Same cold-node
  single test: 3+ minutes → **7s, zero leftover processes**. Full BuildSmoke: 62s, 10/10.
- **Unrelated local noise that made this harder to see:** three `RepoHygieneTests` were permanently red on
  this machine because `.claude/worktrees/Gum` is a directory symlink to the sibling Gum checkout and
  `Directory.EnumerateFiles` follows it. `.claude` is now in that test's ignore list.
- Not reproduced: the "testhost process crashed" abort. It did not recur in any run here, including the
  cold-node hang. Most likely the same bug seen through `--blame`'s eyes (a wedged host being torn down),
  but that is unconfirmed — if it reappears **with the fix in**, it is a genuinely separate defect and this
  entry should not be trusted to cover it.

## 2026-08-07 — `LiveGameProcessTests` (`Category=LiveGame`) hangs the full suite, passes standalone (issue #2005 follow-up)

Found while running the fast suite for an unrelated PropertyGrid fix (#2003) — not caused by that change.

- Symptom: `dotnet test --filter "Category!=BuildSmoke" --no-build` hangs indefinitely. `LiveGameProcessTests`
  (new in #2005, "add a live-game test harness") is tagged `[Trait("Category", "LiveGame")]`, **not**
  `BuildSmoke`, so the standard fast-suite filter (which only excludes `BuildSmoke`) does not skip it.
- Confirmed genuine hang, not slow: `dotnet-stack report -p <testhost pid>` sampled 20s apart showed
  `TotalProcessorTime` byte-identical (5.203125s both times) — zero CPU progress, not just a long-running
  computation. Both samples show the STA thread inside `Xunit.StaFact.UISynchronizationContext.PumpMessages`
  → `TryOneWorkItem` → `Monitor.Wait`, i.e. pumping but waiting on a continuation that never arrives.
- **Run in isolation, `LiveGameProcessTests` passes**: `--filter "FullyQualifiedName~LiveGameProcessTests"`
  → 2/2 green in 22s. The hang only reproduces as part of the full suite, so it's an interaction/ordering
  issue (shared static state, a leftover process/port from an earlier test, or similar) — not a bug in the
  test's own logic in isolation.
- **Workaround for running the fast suite until this is fixed:** add `&Category!=LiveGame` to the filter:
  `--filter "Category!=BuildSmoke&Category!=LiveGame"` → 304/304 green in 27s, normal timing.
- Not root-caused — filed as its own issue (#2008) rather than folded into #2003's PR, since it predates
  and is unrelated to that fix.

## 2026-08-07 — RESOLVED: `LiveGameProcessTests` full-suite hang (#2008), root cause and two distinct symptoms

Follow-up to the entry above. Reproduced repeatedly with `dotnet test --filter "Category!=BuildSmoke"
--no-build` in a fresh worktree — the hang is real and order-dependent, not a fluke: across ~6 back-to-back
runs it hung twice, passed clean three times, and once completed but with 8 unrelated tests
(`WizardProjectLogicAddGameScreenTests`, `GumProjectCreationTests`, `EntityInputMovementTests`) failing on a
`FileNotFoundException`/missing-variable pattern that only appeared when `LiveGameProcessTests` ran earlier
in the same process. Confirmed (independently, from a different concurrent session on a different worktree)
that the hang still reproduced on an unmodified checkout while this investigation was in progress.

- **Root cause, found by temporary file-based logging** (`CommandSender.DiagLog`, self-serve-logging style
  — never asked the terminal to relay anything) around the connect-wait loop, the send semaphore, and
  `GlueTestBootstrap.EnsureHeadlessProjectLoadReady`'s one-time `SynchronizationContext.SetSynchronizationContext(null)`
  call: that line only runs once, guarded by a static `_headlessProjectLoadReady` flag, the first time
  *any* test in the whole process reaches it. Its own comment already flags the intent ("this method may be
  the first thing a different (STA) test thread calls, so clear this thread's context too") — but when the
  STA thread it lands on is an `[StaFact]` test's, `SynchronizationContext.Current` at that point isn't
  stale WinForms garbage, it's `Xunit.StaFact`'s own `UISynchronizationContext`, the pump every subsequent
  `await` in that test needs to resume. Nulling it doesn't fail loudly — the awaited `Task` still eventually
  completes (its continuation just runs on a thread-pool thread instead), but StaFact's own outer pump
  (`PumpTill`/`PumpMessages`/`TryOneWorkItem`) is watching for work items posted to *its* context
  specifically, gets none, and sits in `Monitor.Wait` forever. Confirmed directly: one hung run's diagnostic
  log showed `EditorTest1_SelectingEntity_...` entering `LiveGameProcess.StartAsync`'s connect-wait loop and
  never leaving it, with no exception, no timeout, no further log lines — matching the `dotnet-stack`
  signature from the entry above exactly (STA thread parked in `TryOneWorkItem`/`Monitor.Wait`, every other
  thread idle). Whether this STA thread is the *first* in the process to hit
  `EnsureHeadlessProjectLoadReady` depends on `LiveGameProcessTests`' position in xUnit's default (hash-of-
  test-case-ID) execution order relative to every other `[StaFact]` test that also calls it
  (`ScreenDefaultLayerCodeGenerationTests`, `GoldProjectCompileTests`) — hence "sometimes hangs, sometimes
  doesn't," and the same shape as `LiveEditEmbedLastOrderer`'s already-documented GumPlugin/TaskManager
  landmine (likely a second manifestation of this same class of bug, not root-caused further here).
- **The 8-failure run is a second, separate symptom of the same underlying problem**: `LiveGameProcessTests`
  loads a real gold project into process-wide Glue statics (`GlueState`, `FileManager.RelativeDirectory`,
  registers real plugins) via the same machinery `GoldProjectCompileTests` uses, then deletes its temp
  project directory on `Dispose()`. Nothing about that is scoped to `LiveGameProcessTests` alone — any test
  that runs afterward and relies on that state being clean can inherit stale references. `LiveEditEmbedLastOrderer`
  only orders test *cases* within one class; nothing stopped a completely different class from running
  right after `LiveGameProcessTests` at the assembly level.
- **Fix**: a new assembly-level `ITestCollectionOrderer` (`LiveGameTestsLastCollectionOrderer`, registered
  via `[assembly: TestCollectionOrderer(...)]` in `AssemblyInfo.cs`) forces the collection containing
  `LiveGameProcessTests` to run last, so nothing else in the assembly can ever run after it and inherit
  whatever it leaves dirty — the same fix shape as `LiveEditEmbedLastOrderer`, just at collection scope
  instead of test-case scope. Did not touch the `SynchronizationContext.SetSynchronizationContext(null)`
  line itself — genuinely fixing that would mean auditing every `[StaFact]` test's call order project-wide,
  which is a bigger change than this issue's scope; ordering `LiveGameProcessTests` last sidesteps it
  because by the time its tests run, some earlier test has always already tripped the one-time guard.
- **Also fixed in passing**: `GameConnectionManager.ReceiveString` (Glue-side of the live-game socket
  protocol) awaited the game's response with no timeout at all — `TimeoutInSeconds` was declared
  (`= 10`) but never actually used anywhere in the file. A game that stops responding mid-request (crash,
  deadlock in its own embedded `CommandReceiver`) would hang the caller forever with zero CPU, no thread
  blocked. Not the cause of this specific hang (confirmed via the same diagnostic logging — every hung run
  died in the connect-wait loop, never reached a response-wait), but a real, independently-reachable
  production bug worth having fixed regardless.
- **Verified**: 3 consecutive full `--filter "Category!=BuildSmoke"` runs green (306/306, ~44-50s each) with
  the fix in, plus `LiveGameProcessTests` alone still green (2/2, 18s). Given the bug's own intermittency,
  3-for-3 is reassuring but not proof it can never recur through some other ordering-dependent path — if a
  hang or a similar cross-test-corruption failure resurfaces after this fix, it is a distinct occurrence of
  the same underlying class of bug (an `[StaFact]` test being first to touch shared bootstrap state), not a
  regression of this fix, and should get a new entry here.
