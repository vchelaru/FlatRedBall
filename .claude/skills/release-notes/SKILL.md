---
name: release-notes
description: Drafts FRB1 release notes from commits/PRs since the last release. Triggers: cutting an FRB1 release, drafting release notes, /release-notes.
disable-model-invocation: true
---

# FRB1 Release Notes

Drafts the release notes markdown from git history since the previous release. Does **not** bump versions, trigger workflows, or publish the release — that's [[release]]. Only writes the draft.

Modeled on the Gum repo's `gum-monthly-release` skill (same hybrid curated + full changelog format, same fan-out-to-avoid-summarization fix), adapted for one structural difference: **FRB1's history is not PR-only.** A recent 3.5-month window had 88 commits but only 16 carried a `(#N)` PR suffix — the rest are direct commits, mostly by `vchelaru`. So **commits, not PRs, are the canonical unit** here; PR numbers are used opportunistically for the ~15-20% that have one, not as the primary iteration axis.

# Ask first, don't guess

Same collaboration stance as Gum's skill: when categorization, intent, or which commits belong together is unclear, **stop and ask**, or drop it in `## Open Questions`. Don't invent user-impact descriptions from a one-line commit message.

## Step 1: Front-loaded questions

Ask up front, one message, numbered:

1. **Release tag.** Recent convention: `Release_<Month>_<DD>_<YYYY>` (e.g. `Release_September_23_2025`) — some older tags drop the `Release_` prefix, ignore those as precedent.
2. **Previous-release boundary.** Propose the most recent tag from `gh release list --repo vchelaru/FlatRedBall --limit 5` and confirm. **Cross-check it**: if a newer published release exists between the proposed boundary and `main`, flag it before proceeding (using a stale boundary double-counts commits already shipped).
3. **Breaking changes this release?** If yes, get the explanation now (no separate migration-doc convention confirmed for FRB1 yet — ask the user how they want it described, don't assume a doc URL pattern exists).

## Step 2: Gather commits

The canonical set is **every commit** between the boundary tag and `main`, not a PR search:

```bash
git fetch origin --tags -q
git log --no-merges <prev-tag>..origin/NetStandard --format="%H|%an|%s"
```

For the ~15-20% of subjects ending in `(#N)`, that's a real PR — `gh pr view N --repo vchelaru/FlatRedBall --json title,body,author,files` gets richer context if the subject alone is too sparse. For the rest, there is no PR to look up; read the commit itself (`git show <hash>`) for diff context when the subject line alone doesn't say enough.

**Chores are always omitted, no confirmation needed.** A commit with no end-user-visible effect — repo hygiene/dead-code deletion, CI-only changes, test-only additions, doc/process-only edits (including changes to this repo's own skills/CLAUDE.md) — never appears in the public notes and never goes in Open Questions. Judgment calls about *what counts as user-facing* (ambiguous scope, disputed categorization, a commit that's arguably both) still go in Open Questions — this only covers the clear-chore case.

## Step 3: Fan out per-commit-batch expansion to parallel subagents

Same structural fix as Gum's skill, adapted to commits: the main agent never reads the full commit list itself. Batch the canonical commit set into groups of ~8-10, spawn one subagent per batch in parallel, each returning finished user-impact bullets for its batch.

**The FRB1-specific judgment call, different from Gum's PRs-only case:** since commits (not PRs) are the unit, a run of several small sequential commits often builds *one* feature incrementally (e.g. "First pass at collision fixes" → "Improved camera controlling entity to properly use padding..." → "Handle passing back and forth now works"). The subagent should **consolidate** adjacent same-theme commits into one bullet when they clearly form a single unit of work, and keep them **separate** when they're genuinely distinct changes that happen to be adjacent in history. This cuts both ways versus Gum's rule (which only ever expands a roll-up PR into more bullets, never consolidates) — judge each batch on its own merits.

**Subagent prompt should cover:** the batch's commit hashes+subjects+authors, instruction to run `git show <hash>` for any commit whose subject doesn't already say what changed, the consolidate-vs-separate judgment above, and the same user-impact-first clarity bar as Step 5 below. Tell the subagent to mark clear chores (see Step 2) with an empty `bullet` — those get dropped silently, not routed to Open Questions. Return one JSON object per resulting bullet: `{ "commits": ["<hash>", ...], "author": "name", "section": "...", "bullet": "...", "confidence": "high"|"medium"|"low", "note": "..." }`.

## Step 4: Categorize

Recurring sections seen in past releases (confirm against a couple of `gh release view <recent-tag>` calls before drafting, since this list isn't guaranteed exhaustive):

1. **Breaking Changes** — omit if none.
2. **Biggest Changes** — only for genuinely standout releases (some releases have none at all — e.g. June/March 2025 skipped it entirely). Don't force it.
3. **FRB Editor** — Glue/editor-side changes.
4. **Edit Mode** — live-edit-specific changes (seen in at least one release, often absent).
5. **FRB Engine and Code-related** — runtime engine changes.
6. Other sections as the actual content warrants — these aren't a fixed enum, they're what past releases happened to need. Omit any section with nothing in it.
7. **What's Changed** — complete per-commit list, see Step 6.
8. **Full Changelog** — placeholder, see Step 6.

**Attribution:** default primary author is `vchelaru` (79 of 88 commits in the sampled window) — no `(thanks @...)` for their own commits. Everyone else gets `(thanks @author)`, matching the trailing-space style already used in past releases (`(thanks @vicdotexe )`).

## Step 5: Self-review for clarity (mandatory before writing)

Same bar as Gum's skill — re-read every bullet as a user who's never seen the codebase:

- Names the user-visible symptom/capability/scenario, not internal class/method names.
- Fixes state the symptom *and* trigger condition, not just "fixed a bug."
- Features name the scenario the user can now do, not the API added.
- If you can't defend a bullet in a code review, investigate further, move it to Open Questions, or drop it as internal-only.

## Step 6: What's Changed + Full Changelog

Below the curated sections, list every commit in the canonical set (newest first), one line each, for verifiability:

```bash
git log --no-merges <prev-tag>..origin/NetStandard --format="- %s (thanks @author, only if not vchelaru)"
```

Then a placeholder line directly after:

```
PLACEHOLDER!!!! Full Changelog link
```

This is `https://github.com/vchelaru/FlatRedBall/compare/<prev-tag>...<new-tag>` — only resolvable once the new tag exists, filled in after the release is cut, not missing content.

Use the same loud plain-text placeholder style for any spotlight-feature images in Biggest Changes: `PLACEHOLDER!!!! IMAGE/GIF for <feature name>`.

## Step 7: Write, open, walk through Open Questions

Write to `temp/release-notes-YYYY-MM-DD.md` at the repo root (date from the release tag), then open it (`start temp/release-notes-YYYY-MM-DD.md`) and confirm the path in chat.

Work through `## Open Questions` one at a time with the user, editing the file directly as each resolves. The skill is done when the file has only the release notes - no Open Questions block left.

## Related

- [[release]] — the rest of the release pipeline (version bump, workflow triggers, actually cutting the tag and publishing). This skill only produces the notes draft that feeds into that pipeline's release-creation step.
