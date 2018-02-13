using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.Plugins;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using DialogTreePlugin.Views;
using DialogTreePlugin.Controllers;
using FlatRedBall.Glue.SaveClasses;
using DialogTreePlugin.ViewModels;

namespace DialogTreePlugin
{
    [System.ComponentModel.Composition.Export(typeof(PluginBase))]
    public class MainDialogTreePlugin : PluginBase
    {
        MainControl mainControl;
        

        public override string FriendlyName => "Dialog Tree Plugin";

        //The string keys are no longer editable.
        //We can now dynamically size the columns for any number of languages.
        public override Version Version => new Version(1, 1, 0);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            var toReturn = true;

            return toReturn;
        }

        public override void StartUp()
        {
            this.ReactToItemSelectHandler += HandleItemSelected;
            this.ReactToFileChangeHandler += HandleFileChanged;
            this.ReactToNewFileHandler += HandleNewFile;
        }


        private void HandleItemSelected(TreeNode selectedTreeNode)
        {
            bool shouldShow = GlueState.Self.CurrentReferencedFileSave != null && 
                selectedTreeNode?.Tag == GlueState.Self.CurrentReferencedFileSave &&
                GlueState.Self.CurrentReferencedFileSave.Name.EndsWith(".json");

            if(shouldShow)
            {
                if(mainControl == null)
                {
                    mainControl = MainController.Self.GetControl();
                    this.AddToTab(PluginManager.CenterTab, mainControl, "Dialog Tree");
                }
                else
                {
                    this.AddTab();
                }

                MainController.Self.UpdateTo(GlueState.Self.CurrentReferencedFileSave);
            }
            else
            {
                this.RemoveTab();
            }
        }
        private void HandleNewFile(ReferencedFileSave newFile)
        {
            
        }

        private void HandleFileChanged(string fileName)
        {
            if (fileName.EndsWith(MainController.RelativeToGlobalContentLocalizationDbCsvFile))
            {
                MainController.Self.ReactToLocalizationDbChange(fileName);
            }
        }
    }
}
