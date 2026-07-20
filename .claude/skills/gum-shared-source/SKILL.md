---
name: gum-shared-source
description: FRB vs MonoGameGum source-sharing model — same .cs files, separate .csproj, and where the two diverge. Triggers: Compile Include Gum, GumRuntime, MonoGameGum, Cursor, shared source file missing on FRB side.
---

# Gum Shared Source (FRB vs MonoGameGum)

MonoGameGum (in the sibling [[gum-integration]] repo) is the primary, more widely-used consumer of Gum's runtime. FRB reuses the bulk of that same source (roughly 80-90%) rather than forking it, but the two have **separate `.csproj` files** — there is no shared project file. FRB's projects pull individual Gum source files in directly via `<Compile Include>` with a relative path into the sibling repo (e.g. `GumPlugin.csproj` includes `..\..\..\..\..\Gum\GumRuntime\BbCodeParser.cs`), alongside plain `<ProjectReference>`s to fully-shared sub-libraries like `GumCommon`/`GumCore`.

**Gotcha: a new/renamed file in Gum's source does not automatically appear on the FRB side.** Since FRB's `.csproj` lists individual files rather than referencing the folder, adding a file in `MonoGameGum`/`GumRuntime` requires also adding a matching `<Compile Include>` line in the FRB-side `.csproj` (`GumPlugin.csproj`, `FlatRedBall.Forms.*.csproj`, etc.) or it silently won't compile in.

**Gotcha: not everything is shared — some types are FRB's own, same-named or not.** The flagship example is `Cursor`: MonoGameGum has its own `MonoGameGum.Input.Cursor`, but FRB doesn't use it — FRB has its own, older `FlatRedBall.Gui.Cursor` (`Engines\FlatRedBallXNA\FlatRedBall\Gui\Cursor.cs`), bridged to Gum via extension methods (`CursorExtensions.cs`, e.g. `XRespectingGumZoomAndBounds`) instead of sharing the type. Before assuming a Gum API applies on the FRB side, check which repo/namespace it actually lives in.
