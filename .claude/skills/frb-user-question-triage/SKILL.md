---
name: frb-user-question-triage
description: Answering Discord/GitHub user questions about FlatRedBall — search skills→docs→code, cite the docs URL, propose a doc/API fix with confidence, gate edits on approval. Triggers: pasted user question, "how do I", GitHub issue triage, "answer this person".
---

# Answering User Questions

Use when the user pastes a question from someone (Discord text, a GitHub issue, a `#N` reference) or asks you to help answer a person. Goal: a short reply they can paste back, grounded in a citable docs URL, plus a fix suggestion when docs or the API fall short.

## 1. Classify First

- **Usage question** ("how do I…", "does FlatRedBall support…", "why does X happen", "where is Y") → this skill handles it.
- **Bug report, feature request, or concrete task** → fall through to the normal issue-driven workflow (see `CLAUDE.md`'s "Orchestration preferences" and `CLAUDE.local.md`). A usage question can *still* end in an issue — see step 4 — but that's a docs/API gap we surface, not a defect the user reported.

For a **GitHub issue**, read both `gh issue view <num>` and `gh issue view <num> --comments` before deciding — comments often reclassify the ask.

**Then decide the surface.** Unlike Gum, FRB docs aren't a clean tool-vs-code binary — there are several:

| Surface | Docs path | Covers |
|---|---|---|
| Glue / FRB Editor | `glue-reference/` | The editor itself — menus, properties, project management, live edit |
| Engine + Forms code | `api/flatredball/`, `api/flatredball-forms/` | Runtime C# APIs |
| Glue plugin/codegen APIs | `api/glue-plugin-api/`, `api/glue-runtime-api/`, `api/gluecontrol/` | Writing Glue plugins, generated-code hooks |
| Gum integration in FRB | `gum/` | Using Gum UI *from FRB* (adding components to entities/screens, layers) — **not** Gum's own docs |
| Other plugins | `tiled-plugin/`, `spine/`, `aseprite/`, `glue-gluevault-component-pages-animationeditor-plugin/` | Plugin-specific tool + code usage, mixed |
| Tutorials | `tutorials/` | Walkthroughs (Beefball, Platformer) |

Tool cues: "in Glue"/"in the editor", a tab/menu/property name, a screenshot. Code cues: C#, a class/method/namespace name, "in code". Community and docs use "Glue", "FRB Editor", and "FlatRedBall Editor" interchangeably — don't treat them as different things.

- **Obvious which surface** → answer for that one only.
- **Not obvious** → treat as possibly both editor and code; check both.
- **Investigation is hard or hinges on what they meant** → ask the user to clarify before digging deep.

## 2. Where to Look: skills orient, docs cite, code confirms

- **Skills** `.claude/skills/` (repo-wide — e.g. `gum-integration`, `glue-live-edit`, `glue-file-watch`, `gum-shared-source`, `achx-format`, `color-operations`) and `FRBDK/Glue/.claude/skills/` (Glue-internals — `glue-project-codegen`, `gum-codegen`, `gluj-versions`, `frb-source-linking`, `glue-unit-test-bootstrap`, `refactor`) **orient** — point at the right doc page/source file. **Never link or paste a skill to a user.**
- **Docs** — sibling repo `..\FlatRedBallDocs` — **cite**. `SUMMARY.md` is the index; that's what produces the URL.
- **Code** (this repo's engine + `FRBDK/Glue`) **confirms** real behavior and locates where a doc *should* exist or where the API is wrong.

If a skill or code answers the "why" but **no doc does**, that's a docs-gap signal (outcome C below).

## 3. The Four Outcomes

| Found | Outcome |
|---|---|
| **A. Clear doc answer** | Answer the user, prepend the docs URL. Done. |
| **B. Weak doc answer** (correct but confusing, buried, missing a gotcha) | Answer + propose a doc improvement. |
| **C. Answer only in code, docs missing** | Answer from code + propose where in `FlatRedBallDocs` it should be documented. |
| **D. Code is the problem** (confusing/contradictory names, missing or broken feature) | Answer honestly + propose an API fix. Most likely to become an issue. |

## 4. Always Produce

1. **A paste-able reply** — a quick message a busy human types in Discord, not a document. Lead with the answer, keep it to a few plain sentences, include the docs URL when one exists. Point to [Discord](https://discord.gg/dg7WsFv) or a GitHub issue only when genuinely useful.
   - **Tone: terse and a little lazy, not polished.** Failure mode is sounding like AI: no bold, headings, or bullet lists in the reply — plain sentences only. No reassurance filler ("no worries", "great question"). No em dashes — use a comma, parentheses, or two sentences. Keep code formatting (inline `code`, fenced blocks).
   - **Deliver it as a file, not inline** — write to a temp `.md` path and open it (`Start-Process <path>`) so the user gets clean copy-able source. Keep the terminal response to working notes + a short summary that links the file.
2. **Confidence + justification** for any doc/API fix (rubric below).
3. **An issue** — only for outcomes C/D, and only *after* the user agrees. Then `gh issue create`, per `CLAUDE.md`'s "Orchestration preferences" (issue/branch/PR conventions already documented there — don't duplicate).

## Confidence Rubric

- **High** — must justify: cite the contradicting doc/code, the broken behavior, or the duplicated/missing content.
- **Medium** — plausible improvement, some judgment involved (wording, placement).
- **Low** — a hunch worth raising; flag the uncertainty.

## Approval Gate

Propose first, act after sign-off. **Do not** edit `FlatRedBallDocs`, change code, or run `gh issue create` until the user agrees.

## Building the Docs URL

**Don't fetch docs.flatredball.com to look something up or verify a page** — the live GitBook site is hard for an agent to navigate reliably (redirects, JS-rendered nav). Read the markdown source directly in the sibling `..\FlatRedBallDocs` repo, then construct the citation URL by pattern — no fetch needed to confirm it's right.

Published base: `https://docs.flatredball.com/flatredball/`. Path mirrors the `FlatRedBallDocs` repo tree (drop `.md`):

- `glue-reference/entities/movetolayer.md` → `https://docs.flatredball.com/flatredball/glue-reference/entities/movetolayer`
- A folder's `README.md` → the folder path itself: `api/README.md` → `.../flatredball/api`
- Heading anchor = heading lowercased, spaces→`-`, punctuation stripped.

**Landmine:** `https://docs.flatredball.com/gum/` is a *different* GitBook space entirely — Gum's own docs (its `docs/` folder, in the Gum repo). This repo's own `gum/` folder (FRB-side "how to use Gum from FlatRedBall") publishes under `.../flatredball/gum/`, not `.../gum/`. Don't cite one when you mean the other — a question about e.g. `AttachToContainer` or per-Screen Gum wiring belongs to `flatredball/gum/`; a question about Gum layout/state/binding itself belongs to the separate `gum/` space (see [[gum-integration]] for the boundary between the two codebases).

## Pointers

- Gum-in-FRB architecture (plugin structure, `.gumx`, runtime wrapper) → [[gum-integration]].
- Issue/branch/worktree/PR flow → `CLAUDE.md` "Orchestration preferences", `CLAUDE.local.md`.
- Domain skills are your first-pass index in step 2 — both `.claude/skills/` and `FRBDK/Glue/.claude/skills/`.
