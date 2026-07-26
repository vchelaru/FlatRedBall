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
    /// swappable <see cref="Show"/> delegate so <see cref="VisualStudioProject.ResolveDuplicateProjectEntry"/>'s
    /// branch logic can be unit tested without a live WPF window. The default implementation goes through
    /// <see cref="DialogService.ShowChoice{TResult}"/>, which already marshals to the UI thread even when the
    /// caller (e.g. a project reload queued via TaskManager) is running on a background thread.
    /// </summary>
    public static class DuplicateProjectEntryDialog
    {
        public static Func<string, DuplicateProjectEntryResolution> Show { get; set; } = ShowWpfDialog;

        private static DuplicateProjectEntryResolution ShowWpfDialog(string itemInclude)
        {
            var message = "The item " + itemInclude + " is part of " +
                "the project twice.  Glue does not support double-entries in a project.  What would you like to do?";

            // If the window is closed without a button click (e.g. Escape), ShowChoice returns
            // default(DuplicateProjectEntryResolution), which is RemoveDuplicate (value 0) - this preserves
            // the original behavior of silently removing the duplicate.
            return DialogService.ShowChoice(message,
                ("Remove the duplicate entry and continue", DuplicateProjectEntryResolution.RemoveDuplicate),
                ("Remove the duplicate, but show me a list of all contained objects before removal", DuplicateProjectEntryResolution.RemoveDuplicateAndShowList),
                ("Cancel loading the project - this will throw an exception", DuplicateProjectEntryResolution.CancelLoad));
        }
    }
}
