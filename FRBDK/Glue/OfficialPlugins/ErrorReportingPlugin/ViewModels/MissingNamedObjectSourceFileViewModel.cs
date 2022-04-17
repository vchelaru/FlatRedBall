using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.ErrorReportingPlugin.ViewModels
{
    internal class MissingNamedObjectSourceFileViewModel : ErrorViewModel
    {
        string uniqueId;
        string sourceFile;

        NamedObjectSave NamedObjectSave;

        public override string UniqueId => uniqueId;

        public MissingNamedObjectSourceFileViewModel(NamedObjectSave nos)
        {
            sourceFile = nos.SourceFile;

            Details = $"Missing SourceFile for {nos} : {sourceFile}";
            uniqueId = Details;
            NamedObjectSave = nos;
        }

        public override bool ReactsToFileChange(FilePath filePath)
        {
            var container = NamedObjectSave.GetContainer();

            var referencedFile = container?.GetReferencedFileSaveRecursively(sourceFile);

            var referencedFileFullPath = referencedFile != null
                ? GlueCommands.Self.GetAbsoluteFilePath(referencedFile)
                : null;
            return referencedFileFullPath == filePath;
        }

        public override void HandleDoubleClick()
        {
            GlueState.Self.CurrentNamedObjectSave = NamedObjectSave;
            GlueCommands.Self.DialogCommands.FocusTab("Properties");
        }

        public override bool GetIfIsFixed()
        {
            if (NamedObjectSave.SourceType != SourceType.File)
            {
                return true;
            }
            if (NamedObjectSave.SourceFile != sourceFile)
            {
                return true;
            }

            var container = NamedObjectSave.GetContainer();
            if (container == null)
            {
                return true;
            }
            if (NamedObjectSave.IsDisabled)
            {
                return true;
            }
            if (AvailableFileStringConverter.GetAvailableOptions(container, true, false).Contains(sourceFile))
            {
                return true;
            }
            return false;
        }
    }
}
