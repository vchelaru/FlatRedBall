---
name: glue-cli
description: "Editing a FlatRedBall Glue project from the command line with GlueCli.exe. Triggers: GlueCli, .gluj, .glsj or .glej files, adding a screen, entity, object, variable, state or file to a Glue project, errors in .Generated.cs files."
---

# GlueCli

`GlueCli.exe` makes the same edits the FlatRedBall Editor makes, with no window, and reports any errors the edit added. Use it for every change to a Glue project instead of editing the `.gluj`, `.glsj` and `.glej` files by hand. The reference is [GlueCli](https://docs.flatredball.com/flatredball/glue-reference/gluecli), and [GlueCli for AI Agents](https://docs.flatredball.com/flatredball/glue-reference/gluecli/gluecli-for-ai-agents) is the full agent recipe. `GlueCli --help` lists the commands and `GlueCli <command> --help` gives each one's options.

`GlueCli.exe` sits next to `GlueFormsCore.exe` (the `Xna 4 Tools` folder of the FRBDK download) and runs on Windows only. See [Getting GlueCli](https://docs.flatredball.com/flatredball/glue-reference/gluecli#getting-gluecli).

## The Loop

1. Run `GlueCli regen <project>` once after cloning or pulling.
2. Run `GlueCli describe <project> --json` before editing, and `--element <name>` for one screen or entity. It gives the exact names the other commands take.
3. Make one GlueCli run per edit with `--json`, and branch on `exitCode` (see [Exit Codes](https://docs.flatredball.com/flatredball/glue-reference/gluecli#exit-codes)). `--dry-run` previews an edit. For several edits, put one command per line in a file and run [`batch`](https://docs.flatredball.com/flatredball/glue-reference/gluecli/batch), which loads the project once, then checks for errors, saves and regenerates once after the last line. The batch's `errors` are reported once, for the whole batch, not per line; run commands singly to see which one adds an error. If any line fails or the batch adds errors, the batch writes nothing, so fix the lines and rerun the whole batch.
4. Build the game with `dotnet build` and fix errors as [Build the Game and Fix Errors](https://docs.flatredball.com/flatredball/glue-reference/gluecli/gluecli-for-ai-agents#id-5.-build-the-game-and-fix-errors) describes.

## Landmines

- Edit commands regenerate only what they change, and `check` and `describe` regenerate nothing, so after a clone, a pull or a hand edit only `regen` makes the game build.
- Passing a folder as `<project>` fails when the repo holds a second `.gluj`, such as a copy for another platform. Pass the `.gluj` path.
- Never edit a `.Generated.cs` file. Code generation overwrites it. Fix the project with a GlueCli command, then run `regen`.
- A hand edit to a `.gluj`, `.glsj` or `.glej` file skips the editor's rules. Run `check`, then `regen`.
- Rename with `rename`, `rename-state` or `rename-state-category`. Deleting and re-adding loses every reference to the old name.
- `rename` does not change hand-written code. Update code that uses the old name yourself.
- The FlatRedBall Editor can stay open while GlueCli runs. It reloads the changed files.
