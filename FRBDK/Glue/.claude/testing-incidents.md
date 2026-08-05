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
