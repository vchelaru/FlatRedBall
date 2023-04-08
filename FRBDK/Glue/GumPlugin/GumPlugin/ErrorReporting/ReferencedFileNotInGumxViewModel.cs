using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using Gum.DataTypes;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GumObjectFinder = Gum.Managers.ObjectFinder;
using FrbObjectFinder = FlatRedBall.Glue.Elements.ObjectFinder;
using Xceed.Wpf.AvalonDock.Themes;

namespace GumPluginCore.ErrorReporting
{
    internal class ReferencedFileNotInGumxViewModel : ErrorViewModel
    {
        public override string UniqueId => Details;

        FilePath absoluteFilePath;

        public ReferencedFileNotInGumxViewModel(FilePath absoluteFile)
        {
            var relativeToFrbProject = absoluteFile.RelativeTo(GlueState.Self.CurrentGlueProjectDirectory);
            this.absoluteFilePath = absoluteFile;
            Details = $"{relativeToFrbProject} is referenced by Glue but not in the Gum project";
        }

        public override bool GetIfIsFixed()
        {
            var gumProject = AppState.Self.GumProjectSave;
            if (gumProject == null)
            {
                // Gum has been removed.
                return true;
            }

            var extension = absoluteFilePath.Extension;

            ElementSave element = null;

            if(extension == "gusx")
            {
                var screenName = absoluteFilePath.RemoveExtension().RelativeTo(AppState.Self.GumProjectFolder + "Screens/");
                element = GumObjectFinder.Self.GetScreen(screenName);
            }

            if (element != null)
            {
                // the element has been re-added
                return true;
            }

            var allRfses = FrbObjectFinder.Self.GetAllReferencedFiles();
            var matching = allRfses.FirstOrDefault(item => GlueCommands.Self.GetAbsoluteFileName(item) == absoluteFilePath);

            if(matching == null)
            {
                // RFS has been removed, so there's no more problem....
                return true;
            }

            return false;
        }
    }
}
