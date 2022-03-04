using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.ErrorReportingPlugin.ViewModels
{
    internal class MissingNamedObjectSourceNameErrorViewModel : ErrorViewModel
    {
        string uniqueId;
        string sourceFile;
        string sourceName;

        NamedObjectSave NamedObjectSave;
        public override string UniqueId => uniqueId;

        public MissingNamedObjectSourceNameErrorViewModel(NamedObjectSave nos)
        {
            sourceName = nos.SourceName;
            sourceFile = nos.SourceFile;

            Details = $"Missing SourceObject for {nos} : {sourceName}";
            uniqueId = Details;
            NamedObjectSave = nos;

        }

        public override bool ReactsToFileChange(FilePath filePath)
        {
            var container = NamedObjectSave.GetContainer();

            var referencedFile = container?.GetReferencedFileSaveRecursively(sourceFile);

            var referencedFileFullPath = referencedFile!= null 
                ? GlueCommands.Self.GetAbsoluteFilePath(referencedFile)
                : null;
            return referencedFileFullPath == filePath;
        }

        public override void HandleDoubleClick() => GlueState.Self.CurrentNamedObjectSave = NamedObjectSave;

        public override bool GetIfIsFixed()
        {
            if(NamedObjectSave.SourceType != SourceType.File)
            {
                return true;
            }
            if(NamedObjectSave.SourceFile != sourceFile)
            {
                return true;
            }
            if(NamedObjectSave.SourceName != sourceName)
            {
                return true;
            }
            if(NamedObjectSave.GetContainer() == null)
            {
                return true;
            }
            if(NamedObjectSave.IsDisabled)
            {
                return true;
            }
            if(NamedObjectSave.IsEntireFile)
            {
                return true; // can condition even happen? Currently no, but just in case there's some logic bug, we'll check this.
            }
            if(AvailableNameablesStringConverter.GetAvailableNamedObjectSourceNames(NamedObjectSave).Contains(sourceName))
            {
                return true;
            }
            return false;
        }
    }
}
