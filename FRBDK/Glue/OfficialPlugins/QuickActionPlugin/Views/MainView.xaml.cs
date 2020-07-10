using FlatRedBall.Glue.Plugins.ExportedImplementations;
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
        public MainView()
        {
            InitializeComponent();
        }

        private void AddScreenButton_Clicked(object sender, RoutedEventArgs e)
        {
            GlueCommands.Self.DialogCommands.ShowAddNewScreenDialog();
        }

        private void AddLevelButton_Clicked(object sender, RoutedEventArgs e)
        {
            var gameScreen = GlueState.Self.CurrentGlueProject.Screens.FirstOrDefault(
                item => item.Name == "Screens\\GameScreen");

            if(gameScreen == null)
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox("Could not find screen called GameScreen to use as the base screen for a new level screen.");

            }
            GlueCommands.Self.DialogCommands.ShowCreateDerivedScreenDialog(gameScreen);
        }

        private void AddEntityButton_Clicked(object sender, RoutedEventArgs e)
        {
            GlueCommands.Self.DialogCommands.ShowAddNewEntityDialog();
        }

        private void CreateNewProjectButton_Clicked(object sender, RoutedEventArgs e)
        {
            GlueCommands.Self.ProjectCommands.CreateNewProject();
        }
    }
}
