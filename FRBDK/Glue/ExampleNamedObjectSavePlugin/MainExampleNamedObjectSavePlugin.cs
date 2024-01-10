using ExampleNamedObjectSavePlugin.ViewModels;
using ExampleNamedObjectSavePlugin.Views;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.SaveClasses;
using System.ComponentModel.Composition;

namespace ExampleNamedObjectSavePlugin
{
    [Export(typeof(PluginBase))]
    public class MainExampleNamedObjectSavePlugin : PluginBase
    {
        private ExampleViewModel _viewModel;
        private PluginTab _tab;

        public override string FriendlyName => "Example Named Object Plugin";

        public override void StartUp()
        {
            ReactToItemSelectHandler += HandleItemSelected;
        }

        private void HandleItemSelected(ITreeNode selectedTreeNode)
        {
            if (selectedTreeNode?.Tag is NamedObjectSave namedObjectSave)
            {
                // Let's show this only if the user selects a circle:
                var shouldShowUi =
                    namedObjectSave?.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Circle;

                if (shouldShowUi)
                {
                    if (_viewModel == null)
                    {
                        CreateViewAndViewModel();
                    }
                    _viewModel.GlueObject = namedObjectSave;
                    _viewModel.UpdateFromGlueObject();

                    _tab.Show();
                }
                else
                {
                    _tab?.Hide();
                }
            }
        }

        private void CreateViewAndViewModel()
        {
            _viewModel = new ExampleViewModel();
            var view = new ExampleView() { DataContext = _viewModel };
            _tab = CreateAndAddTab(view, "Example Plugin");
        }
    }
}