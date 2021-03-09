using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.SongPlugin.CodeGenerators;
using OfficialPlugins.SongPlugin.ViewModels;
using OfficialPlugins.SongPlugin.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OfficialPlugins.SongPlugin
{
    [Export(typeof(PluginBase))]
    public class MainSongPlugin : PluginBase
    {
        #region Fields/Properties

        MainSongControlViewModel viewModel;
        MainSongControl control;
        PluginTab pluginTab;


        public override string FriendlyName
        {
            get { return "Song Plugin"; }
        }

        public override Version Version
        {
            get { return new Version(1, 0); }
        }

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            viewModel = new MainSongControlViewModel();

            AdjustAssetTypeInfo();

            CreateCodeGenerator();

            AssignEvents();
        }

        private void AdjustAssetTypeInfo()
        {
            var atis = AvailableAssetTypes.Self.AllAssetTypes
                .Where(item => item.QualifiedRuntimeTypeName.QualifiedType ==
                    "Microsoft.Xna.Framework.Media.Song");

            foreach(var ati in atis)
            {
                ati.CustomLoadFunc = (element, nos, rfs, contentManager) =>
                {
                    bool shouldAssignField = ReferencedFileSaveCodeGenerator.NeedsFullProperty(rfs, element);

                    string variableName;

                    if (shouldAssignField)
                    {
                        variableName = "m" + rfs.GetInstanceName();
                    }
                    else
                    {
                        variableName = rfs.GetInstanceName();
                    }

                    var fileName = ReferencedFileSaveCodeGenerator.GetFileToLoadForRfs(rfs, ati); // FlatRedBall.IO.FileManager.RemoveExtension(rfs.Name).ToLowerInvariant().Replace("\\", "/");
                    if(rfs.DestroyOnUnload == false)
                    {
                        contentManager = "FlatRedBall.FlatRedBallServices.GlobalContentManager";

                    }
                    //return $"{propertyName} = FlatRedBall.FlatRedBallServices.Load<Microsoft.Xna.Framework.Media.Song>(@"content/screens/gamescreen/baronsong", contentManagerName);";
                    return $"{variableName} = FlatRedBall.FlatRedBallServices.Load<Microsoft.Xna.Framework.Media.Song>(@\"{fileName}\", {contentManager});";
                };

            }
                
        }

        private void CreateCodeGenerator()
        {
            var codeGenerator = new SongPluginCodeGenerator();
            this.RegisterCodeGenerator(codeGenerator);
        }

        private void AssignEvents()
        {
            this.ReactToItemSelectHandler += HandleItemSelected;
        }

        private void HandleItemSelected(TreeNode selectedTreeNode)
        {
            var rfs = GlueState.Self.CurrentReferencedFileSave;

            viewModel.GlueObject = rfs;


            bool shouldShowControl = false;
            if (rfs != null && rfs.GetAssetTypeInfo()?.QualifiedRuntimeTypeName.QualifiedType == "Microsoft.Xna.Framework.Media.Song")
            {
                viewModel.UpdateFromGlueObject();
                shouldShowControl = true;
            }

            if(shouldShowControl)
            {
                if (control == null)
                {
                    control = new MainSongControl();
                    pluginTab = this.CreateTab(control, "Song");
                    control.DataContext = viewModel;
                }
                pluginTab.Show();
            }
            else
            {
                pluginTab?.Hide();
            }
        }
    }
}
