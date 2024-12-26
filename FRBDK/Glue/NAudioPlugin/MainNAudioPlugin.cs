using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Utilities;
using FlatRedBall.Glue.VSHelpers;
using NAudioPlugin.CodeGenerators;
using NAudioPlugin.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace NAudioPlugin
{
    [Export(typeof(PluginBase))]
    public class MainNAudioPlugin : PluginBase
    {
        public override string FriendlyName => "NAudio Plugin";

        IGlueState _glueState;

        public MainNAudioPlugin()
        {
            _glueState = GlueState.Self;
        }

        public override void StartUp()
        {
            RegisterCodeGenerator(new ElementCodeGenerator());

            AddMenuItemTo("Embed NAudio Classes", Localization.MenuIds.EmbedNAudioClassesId, HandleEmbedNAudioFiles, Localization.MenuIds.ContentId);
            AssignEvents();
        }

        private void AssignEvents()
        {
            this.ReactToLoadedGluxEarly += HandleGluxLoadedEarly;
            this.ReactToChangedPropertyHandler += HandleChangedProperty;

        }

        private void HandleChangedProperty(string changedMember, object oldValue, GlueElement owner)
        {
            var file = _glueState.CurrentReferencedFileSave;

            if(file?.GetAssetTypeInfo() == AssetTypeInfoManager.NAudioMp3SongAti)
            {
                HandleEmbedNAudioFiles(null, null);
            }
        }

        private void HandleGluxLoadedEarly()
        {
            // Do this on every glux load so that we can add the ati according to the glux version #
            Managers.AssetTypeInfoManager.ResetAssetTypes();

            // Does this have any NAudio files? If so, let's embed:
            var hasNAudioFiles = ObjectFinder.Self.GetAllReferencedFiles().Any(item => item.RuntimeType == Managers.AssetTypeInfoManager.NAudioQualifiedType);

            if(hasNAudioFiles)
            {
                HandleEmbedNAudioFiles(null, null);
            }

        }

        private void HandleEmbedNAudioFiles(object sender, EventArgs e)
        {
            TaskManager.Self.Add(() =>
            {
                var codeItemAdder = new CodeBuildItemAdder();
                codeItemAdder.OutputFolderInProject = "NAudio";
                var thisAssembly = this.GetType().Assembly;

                codeItemAdder.AddFolder("NAudioPlugin/Embedded", thisAssembly);

                codeItemAdder.PerformAddAndSaveTask(thisAssembly);

                var nugetPackageName = "NAudio";
                GlueCommands.Self.ProjectCommands.AddNugetIfNotAdded(nugetPackageName, "2.1.0");

                GlueCommands.Self.ProjectCommands.SaveProjects();

            }, nameof(HandleEmbedNAudioFiles));

        }
    }
}
