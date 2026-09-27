using GlueControl.Editing;
using Shouldly;

namespace EngineUnitTests.GlueControl;

// GumCursorLogic is compiled directly from Glue's embedded-code source (see EngineUnitTests.csproj).
// Issue #2339: a full-screen Gum panel made every world object in edit mode unselectable, because any
// Gum element under the cursor blocked selection.
public class GumCursorLogicTests
{
    [Fact]
    public void ShouldGumBlockWorldSelection_GumInteractionDisabled_ReturnsFalseOverWindow()
    {
        GumCursorLogic.ShouldGumBlockWorldSelection(
            isGumInteractionEnabled: false, isCursorUsingMouse: true, isCursorOverWindow: true)
            .ShouldBeFalse();
    }

    [Fact]
    public void ShouldGumBlockWorldSelection_GumInteractionEnabled_ReturnsTrueOverWindow()
    {
        GumCursorLogic.ShouldGumBlockWorldSelection(
            isGumInteractionEnabled: true, isCursorUsingMouse: true, isCursorOverWindow: true)
            .ShouldBeTrue();
    }

    [Fact]
    public void ShouldGumBlockWorldSelection_GumInteractionEnabled_ReturnsFalseWhenNotOverWindow()
    {
        GumCursorLogic.ShouldGumBlockWorldSelection(
            isGumInteractionEnabled: true, isCursorUsingMouse: true, isCursorOverWindow: false)
            .ShouldBeFalse();
    }
}
