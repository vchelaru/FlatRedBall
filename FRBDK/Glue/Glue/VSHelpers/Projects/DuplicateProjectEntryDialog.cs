using System;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Managers;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public enum DuplicateProjectEntryResolution
    {
        RemoveDuplicate,
        RemoveDuplicateAndShowList,
        CancelLoad
    }

    /// <summary>
    /// Asks the user how to resolve a duplicate project entry found while loading a .csproj. Shown behind a
    /// swappable <see cref="Show"/> delegate (same seam shape as <see cref="TaskManager.UiThreadMarshaller"/>)
    /// so <see cref="VisualStudioProject.ResolveDuplicateProjectEntry"/>'s branch logic can be unit tested
    /// without a live WPF window, and so the WPF construction always happens on the UI thread even when the
    /// caller (e.g. a project reload queued via TaskManager) is running on a background thread.
    /// </summary>
    public static class DuplicateProjectEntryDialog
    {
        public static Func<string, DuplicateProjectEntryResolution> Show { get; set; } = ShowWpfDialog;

        private static DuplicateProjectEntryResolution ShowWpfDialog(string itemInclude)
        {
            return TaskManager.Self.OnUiThread(() =>
            {
                var mbmb = new MultiButtonMessageBoxWpf();

                mbmb.MessageText = "The item " + itemInclude + " is part of " +
                    "the project twice.  Glue does not support double-entries in a project.  What would you like to do?";

                mbmb.AddButton("Remove the duplicate entry and continue", DuplicateProjectEntryResolution.RemoveDuplicate);
                mbmb.AddButton("Remove the duplicate, but show me a list of all contained objects before removal", DuplicateProjectEntryResolution.RemoveDuplicateAndShowList);
                mbmb.AddButton("Cancel loading the project - this will throw an exception", DuplicateProjectEntryResolution.CancelLoad);

                if (mbmb.ShowDialog() == true)
                {
                    return (DuplicateProjectEntryResolution)mbmb.ClickedResult;
                }

                // Window closed without a button click (e.g. Escape) - preserve the original behavior of
                // silently removing the duplicate.
                return DuplicateProjectEntryResolution.RemoveDuplicate;
            });
        }
    }
}
