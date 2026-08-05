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
