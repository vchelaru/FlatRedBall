using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using OfficialPlugins.AnimationChainPlugin.Errors;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Windows.Navigation;
using FileManager = ToolsUtilities.FileManager;

namespace OfficialPluginsCore.AnimationChainPlugin
{
    [Export(typeof(PluginBase))]
    public class MainAnimationChainPlugin : PluginBase
    {
        #region Fields/Properties

        public override string FriendlyName => "Animation Chain Plugin";

        public override Version Version => new Version(1, 0);

        

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            AssignEvents();
            this.AddErrorReporter(new AnimationChainErrorReporter());
        }

        private void AssignEvents()
        {
            this.ReactToNewFileHandler += HandleNewFile;
            this.ReactToFileChangeHandler += HandleFileChanged;
            this.ReactToNamedObjectChangedValue += HandleNamedObjectChangedValue;
        }

        private void HandleNamedObjectChangedValue(string changedMember, object oldValue, NamedObjectSave namedObject)
        {
            if (changedMember == "CurrentChainName" || changedMember == "AnimationChains")
            {
                this.RefreshErrors();
            }
        }

        private void HandleFileChanged(string fileName)
        {
            var filePath = new FilePath(fileName);
            if (filePath.Extension == "achx")
            {
                this.RefreshErrors();
            }
        }

        private void HandleNewFile(ReferencedFileSave newFile)
        {
            var extension = FileManager.GetExtension(newFile.Name);
            if(extension == "achx")
            {
                var file = GlueCommands.Self.GetAbsoluteFilePath(newFile);

                if(file.Exists())
                {
                    // load it and set the project file
                    var achx = AnimationChainListSave.FromFile(file.FullPath);

                    var projectFile = FileManager.MakeRelative(GlueState.Self.GlueProjectFileName, file.GetDirectoryContainingThis().FullPath);

                    if(projectFile != achx.ProjectFile)
                    {
                        achx.ProjectFile = projectFile;
                        achx.Save(file.FullPath);
                    }
                }
            }
        }
    }
}
