using FlatRedBall.Glue.Plugins.ExportedImplementations;
using OfficialPluginsCore.QuickActionPlugin.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace OfficialPluginsCore.QuickActionPlugin.Managers
{
    class ButtonVisibilityManager
    {
        MainView mainView;
        public ButtonVisibilityManager(MainView mainView)
        {
            this.mainView = mainView;
        }

        public void UpdateVisibility()
        {
            Visibility ToVisibility(bool value)
            {
                if (value) return Visibility.Visible;
                else return Visibility.Collapsed;
            }

            var project = GlueState.Self.CurrentGlueProject;
            var treeNode = GlueState.Self.CurrentTreeNode;
            var gameScreen = project?.Screens.FirstOrDefault(item => item.Name == "GameScreen");
            var selectedObject = treeNode?.Tag;

            mainView.CreateNewProjectButton.Visibility = ToVisibility(
                project == null);

            mainView.AddScreenButton.Visibility = ToVisibility(
                project != null &&
                (selectedObject == null ||
                    project.Screens.Count == 0)
                    );

            mainView.AddLevelButton.Visibility = ToVisibility(
                project != null &&
                (
                    project.Screens.Any(item => item.Name == "Screens\\GameScreen")
                    ));

            mainView.AddEntityButton.Visibility = ToVisibility(
                project != null &&
                (
                    selectedObject == null ||
                    project.Entities.Count == 0 
                )
                );

        }
    }
}
