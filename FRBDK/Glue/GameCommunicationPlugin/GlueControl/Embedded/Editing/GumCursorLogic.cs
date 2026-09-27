namespace GlueControl.Editing
{
    public static class GumCursorLogic
    {
        /// <summary>
        /// Whether Gum UI under the cursor should stop edit mode from selecting world objects. Every Gum
        /// component is a Forms control, so a full-screen HUD panel counts as "over a window" everywhere,
        /// which would make every world object unselectable. Off by default; users who want to click Gum
        /// controls in edit mode turn it on from the Game tab.
        /// </summary>
        public static bool ShouldGumBlockWorldSelection(bool isGumInteractionEnabled, bool isCursorUsingMouse, bool isCursorOverWindow)
        {
            return isGumInteractionEnabled && isCursorUsingMouse && isCursorOverWindow;
        }
    }
}
