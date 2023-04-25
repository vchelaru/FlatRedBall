using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ExampleNamedObjectPlugin
{
    // uncomment this to make this plugin actually get exported...
    //[Export(typeof(PluginBase))]
    public class MainExampleNamedObjectPlugin : PluginBase
    {
        public override string FriendlyName => "Example Named Object Plugin";

        ExampleViewModel ViewModel;
        PluginTab tab;

        public override void StartUp()
        {
            this.ReactToItemSelectHandler += HandleItemSelected;
        }

        private void HandleItemSelected(ITreeNode selectedTreeNode)
        {
            if(selectedTreeNode?.Tag is NamedObjectSave namedObjectSave)
            {
                // Let's show this only if the user selects a circle:
                var shouldShowUi =
                    namedObjectSave?.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Circle;

                if(shouldShowUi)
                {
                    if(ViewModel == null)
                    {
                        CreateViewAndViewModel();
                    }
                    ViewModel.GlueObject = namedObjectSave;
                    ViewModel.UpdateFromGlueObject();

                    tab.Show();

                }
                else
                {
                    tab.Hide();
                }
            }
        }

        private void CreateViewAndViewModel()
        {
            ViewModel = new ExampleViewModel();
            var view = new ExampleView();
            view.DataContext = ViewModel;
            tab = this.CreateTab(view, "Example Plugin");
        }
    }
}
