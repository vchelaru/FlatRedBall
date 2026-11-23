# frb-skills

Skills for game developers building with FlatRedBall.

Copy the subfolders you want into your game repo's `.claude/skills/` (or your user-global `~/.claude/skills/`) to give Claude Code guidance on using FlatRedBall from a game project.

These are **3rd-party skills**: they describe how to *use* FlatRedBall. Skills for working *on* the engine and the FlatRedBall Editor live in `/.claude/skills/` and are not intended for distribution.

In this repo the same files are also surfaced at `/.claude/skills/<name>` via local symlinks (gitignored) so Claude Code's auto-discovery picks them up while working on the engine. The canonical copy is here. Run `scripts/link-skills.ps1` to create the links.
