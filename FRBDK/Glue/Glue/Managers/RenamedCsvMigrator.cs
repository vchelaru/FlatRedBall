using System;
using System.Linq;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.Managers
{
    /// <summary>
    /// Migrates an element's CSV ReferencedFileSave when the plugin that owns it has changed the file
    /// name it generates. The platformer and top down plugins both switch their CSV name on glux
    /// version (PlatformerValues -> PlatformerValuesStatic at CsvInheritanceSupport), and without this
    /// an upgraded project keeps the old file, gets a second one added alongside it, and loses whatever
    /// values were tuned in the original (the view model reads the new name, finds nothing, and writes
    /// defaults back out).
    /// </summary>
    public static class RenamedCsvMigrator
    {
        /// <summary>
        /// Renames element's "oldUnqualified".csv ReferencedFileSave to "newUnqualified".csv, moving the
        /// file, fixing project build items, the owning CustomClassSave, and any CustomVariable default
        /// values that name the old file.
        /// </summary>
        /// <returns>True if a rename happened.</returns>
        public static bool MigrateCsvRename(GlueElement element, string oldUnqualified, string newUnqualified)
        {
            if (element == null || oldUnqualified == newUnqualified)
            {
                return false;
            }

            var oldFileName = oldUnqualified + ".csv";
            var newFileName = newUnqualified + ".csv";

            var oldRfs = element.ReferencedFiles.FirstOrDefault(item =>
                FileManager.RemovePath(item.Name).Equals(oldFileName, StringComparison.OrdinalIgnoreCase));

            if (oldRfs == null)
            {
                return false;
            }

            var newName = FileManager.GetDirectory(oldRfs.Name, RelativeType.Relative) + newFileName;

            // Both present means this project already took the "second csv added alongside" path. Renaming
            // on top of the new one would prompt about overwriting and could destroy whichever the user has
            // since edited, so leave it alone - only the never-migrated case is unambiguous.
            var alreadyHasNew = element.ReferencedFiles.Any(item =>
                item.Name.Equals(newName, StringComparison.OrdinalIgnoreCase));

            if (alreadyHasNew || GlueCommands.Self.GetAbsoluteFilePath(newName).Exists())
            {
                return false;
            }

            var oldRfsName = oldRfs.Name;

            var didRename = GlueCommands.Self.FileCommands.RenameReferencedFileSave(oldRfs, newName);

            if (!didRename)
            {
                return false;
            }

            UpdateCustomClass(oldRfsName, newName);
            UpdateCustomVariableDefaultValues(element, oldFileName, newFileName);

            PluginManager.ReceiveOutput($"Renamed {oldRfsName} to {newName}");

            return true;
        }

        private static void UpdateCustomClass(string oldRfsName, string newRfsName)
        {
            var customClasses = GlueState.Self.CurrentGlueProject?.CustomClasses;

            if (customClasses == null)
            {
                return;
            }

            foreach (var customClass in customClasses)
            {
                if (customClass.CsvFilesUsingThis.Remove(oldRfsName) &&
                    customClass.CsvFilesUsingThis.Contains(newRfsName) == false)
                {
                    customClass.CsvFilesUsingThis.Add(newRfsName);
                }
            }
        }

        /// <summary>
        /// Platformer/top down variables store their default as "Ground in PlatformerValues.csv" - the
        /// " in &lt;file&gt;" suffix is which csv the value came from, and it's what the property grid
        /// matches its dropdown against, so a stale one shows up blank in the editor.
        /// </summary>
        private static void UpdateCustomVariableDefaultValues(GlueElement element, string oldFileName, string newFileName)
        {
            var elements = new[] { element }
                .Concat(ObjectFinder.Self.GetAllElementsThatInheritFrom(element));

            var oldSuffix = " in " + oldFileName;
            var newSuffix = " in " + newFileName;

            foreach (var toUpdate in elements)
            {
                foreach (var variable in toUpdate.CustomVariables)
                {
                    if (variable.DefaultValue is string asString &&
                        asString.EndsWith(oldSuffix, StringComparison.OrdinalIgnoreCase))
                    {
                        variable.DefaultValue = asString.Substring(0, asString.Length - oldSuffix.Length) + newSuffix;
                    }
                }
            }
        }
    }
}
