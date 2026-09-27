---
name: gum-forms
description: FRB Gum Forms codegen and how Forms controls take cursor input. Triggers: XxxForms.Generated.cs, FormsClassCodeGenerator, UserControl, Cursor.WindowOver, clicks eaten by Gum UI.
---

# Gum Forms (FRB1)

FRB's Gum codegen makes two classes for each Gum element: a Visual runtime (`GumRuntimes\XxxRuntime.Generated.cs`) and a Forms class (`Forms\...\XxxForms.Generated.cs`). A Gum screen paired with an FRB screen gets both. MonoGameGum and FRB2 merged the two, so don't carry this split over from them. See [[gum-integration]] for the rest of the Gum plugin.

## Where it lives

| Piece | Location |
|---|---|
| Forms class per element | `FRBDK\Glue\GumPlugin\GumPlugin\CodeGeneration\FormsClassCodeGenerator.cs` |
| Forms instance on an FRB screen | `FormsObjectCodeGenerator.cs` (same folder) |
| Forms controls (Button, UserControl...) | `Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Shared\Controls\` |

## Landmines

- **Every Gum component is a Forms control.** `FormsClassCodeGenerator` makes each component's Forms class inherit `FlatRedBall.Forms.Controls.UserControl`, including plain layout panels. So "is this a Forms control?" says nothing about whether it's interactive.
- **Gum UI under the cursor blocks world selection in live edit only when the Game tab's "Gum Interaction" checkbox is on** (`GumCursorLogic.ShouldGumBlockWorldSelection`). Gum controls still receive the click either way. See [[glue-live-edit]].
