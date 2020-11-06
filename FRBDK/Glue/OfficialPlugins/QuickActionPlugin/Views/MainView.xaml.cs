using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using FlatRedBall.Glue.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OfficialPluginsCore.QuickActionPlugin.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : UserControl
    {
        const string GameScreenName = "Screens\\GameScreen";

        public event Action AnyButtonClicked;

        public MainView()
        {
            InitializeComponent();
        }

        private void AddScreenButton_Clicked(object sender, RoutedEventArgs e)
        {
            GlueCommands.Self.DialogCommands.ShowAddNewScreenDialog();
            AnyButtonClicked();
        }

        private void AddLevelButton_Clicked(object sender, RoutedEventArgs e)
        {
            var gameScreen = GlueState.Self.CurrentGlueProject.Screens.FirstOrDefault(
                item => item.Name == GameScreenName);

            if(gameScreen == null)
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox("Could not find screen called GameScreen to use as the base screen for a new level screen.");

            }
            else
            {
                GlueCommands.Self.DialogCommands.ShowCreateDerivedScreenDialog(gameScreen);
                AnyButtonClicked();
            }
        }

        private void AddEntityButton_Clicked(object sender, RoutedEventArgs e)
        {
            GlueCommands.Self.DialogCommands.ShowAddNewEntityDialog();
            AnyButtonClicked();
        }

        private void CreateNewProjectButton_Clicked(object sender, RoutedEventArgs e)
        {
            GlueCommands.Self.ProjectCommands.CreateNewProject();
            AnyButtonClicked();
        }

        private void AddGumButton_Clicked(object sender, RoutedEventArgs args)
        {
            PluginManager.CallPluginMethod("Gum Plugin", "CreateGumProject");
        }

        private void AddObjectButton_Clicked(object sender, RoutedEventArgs e)
        {
            // deselect the currently selected named object
            if(GlueState.Self.CurrentNamedObjectSave != null)
            {
                GlueState.Self.CurrentNamedObjectSave = null;
            }

            GlueCommands.Self.DialogCommands.ShowAddNewObjectDialog();
            AnyButtonClicked();
        }

        private void AddInstanceOfEntityButton_Clicked(object sender, RoutedEventArgs e)
        {
            var viewModel = new AddObjectViewModel();
            viewModel.SourceType = SourceType.Entity;

            viewModel.SelectedEntitySave = GlueState.Self.CurrentEntitySave;
            viewModel.ObjectName = GlueState.Self.CurrentEntitySave.GetStrippedName() + "Instance";

            var gameScreen = GlueState.Self.CurrentGlueProject.GetScreenSave(GameScreenName);

            var newNos = GlueCommands.Self.GluxCommands.AddNewNamedObjectTo(viewModel, gameScreen, null);

            GlueState.Self.CurrentNamedObjectSave = newNos;

            AnyButtonClicked();
        }

        private void AddListOfEntityButton_Clicked(object sender, RoutedEventArgs e)
        {
            var viewModel = new AddObjectViewModel();
            viewModel.SourceType = SourceType.FlatRedBallType;

            viewModel.SelectedAti = AvailableAssetTypes.CommonAtis.PositionedObjectList;
            viewModel.SourceClassGenericType = GlueState.Self.CurrentEntitySave.Name;
            viewModel.ObjectName = GlueState.Self.CurrentEntitySave.GetStrippedName() + "List";

            var gameScreen = GlueState.Self.CurrentGlueProject.GetScreenSave(GameScreenName);
            var newNos = GlueCommands.Self.GluxCommands.AddNewNamedObjectTo(viewModel, gameScreen, null);

            GlueState.Self.CurrentNamedObjectSave = newNos;

            AnyButtonClicked();
        }

        private void AddEntityFactory_Clicked(object sender, RoutedEventArgs e)
        {
            var entity = GlueState.Self.CurrentEntitySave;

            entity.CreatedByOtherEntities = true;

            EditorObjects.IoC.Container.Get<SetPropertyManager>().ReactToPropertyChanged(
                nameof(entity.CreatedByOtherEntities), false, nameof(entity.CreatedByOtherEntities), null);

            GlueCommands.Self.GluxCommands.SaveGluxTask();

            AnyButtonClicked();
        }
    }
}
