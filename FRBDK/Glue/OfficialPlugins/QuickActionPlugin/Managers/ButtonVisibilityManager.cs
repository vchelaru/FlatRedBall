using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPluginsCore.QuickActionPlugin.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

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

            var glueProject = GlueState.Self.CurrentGlueProject;
            var treeNode = GlueState.Self.CurrentTreeNode;
            var selectedObject = treeNode?.Tag;
            var selectedElement = selectedObject as IElement;
            var selectedEntity = selectedObject as EntitySave;

            var gameScreen = glueProject?.Screens.FirstOrDefault(item => item.Name == "Screens\\GameScreen");
            var hasGameScreen = gameScreen != null;
            var hasGumProject = GlueState.Self.GetAllReferencedFiles()
                .Any(item => item.Name.ToLowerInvariant().EndsWith(".gumx"));

            #region Create New Project

            mainView.CreateNewProjectButton.Visibility = ToVisibility(
                glueProject == null);

            #endregion

            mainView.AddGumProject.Visibility = ToVisibility(!hasGumProject && glueProject != null);

            #region Add Screen Button

            mainView.AddScreenButton.Visibility = ToVisibility(glueProject != null);
            if(hasGameScreen == false)
            {
                mainView.AddScreenButton.Details =
                    "Every game needs at least one screen.\nUsually the game logic is in a screen called GameScreen.";
            }
            else
            {
                mainView.AddScreenButton.Details =
                    "Most games have multiple screens. Examples include title screens, level selection screens, and settings screens.";
            }

            #endregion

            #region Add Entity

            mainView.AddEntityButton.Visibility = ToVisibility(glueProject != null );

            #endregion

            #region Add Object to Screen/Entity

            mainView.AddObjectToEntityButton.Visibility = ToVisibility(
                GlueState.Self.CurrentEntitySave != null
                );
            mainView.AddObjectToEntityButton.Title = $"Add Object to {GlueState.Self.CurrentElement?.GetStrippedName()}";
            mainView.AddObjectToEntityButton.Details = "Common entity object types include sprites and shapes for collision.";

            mainView.AddObjectToScreenButton.Visibility = ToVisibility(
                GlueState.Self.CurrentScreenSave != null
                );
            mainView.AddObjectToScreenButton.Title = $"Add Object to {GlueState.Self.CurrentElement?.GetStrippedName()}";
            mainView.AddObjectToScreenButton.Details = "Common screen object types include entity lists, layers, and collision relationships.";


            #endregion

            #region Add List of Entity to GameScreen
            mainView.AddListOfEntityButton.Visibility = ToVisibility(
                glueProject != null &&
                selectedObject is EntitySave &&
                hasGameScreen &&
                // Individual:
                gameScreen.AllNamedObjects.Any(item => item.SourceClassType == (selectedObject as EntitySave).Name) == false &&
                // List:
                gameScreen.AllNamedObjects.Any(item => item.IsList && item.SourceClassGenericType == (selectedObject as EntitySave).Name) == false 
                );

            if(mainView.AddListOfEntityButton.Visibility == Visibility.Visible)
            {
                var entity = selectedObject as EntitySave;
                var entityName = entity.GetStrippedName();
                mainView.AddListOfEntityButton.Title =
                    $"Add {entityName} List to GameScreen";

                mainView.AddListOfEntityButton.Details =
                    $"Lists enable multiple copies of this entity to exist in the game screen.";
            }

            #endregion

            #region Add Instance of Entity to GameScreen

            mainView.AddInstanceOfEntityButton.Visibility = ToVisibility(
                glueProject != null &&
                selectedObject is EntitySave &&
                hasGameScreen &&
                // Individual:
                gameScreen.AllNamedObjects.Any(item => item.SourceClassType == (selectedObject as EntitySave).Name) == false &&
                // List:
                gameScreen.AllNamedObjects.Any(item => item.IsList && item.SourceClassGenericType == (selectedObject as EntitySave).Name) == false
                );

            if(mainView.AddInstanceOfEntityButton.Visibility == Visibility.Visible)
            {
                var entity = selectedObject as EntitySave;
                var entityName = entity.GetStrippedName();
                mainView.AddInstanceOfEntityButton.Title =
                    $"Add {entityName} Instance to GameScreen";

                mainView.AddInstanceOfEntityButton.Details =
                    $"Adds a single instance to the GameScreen. This is usually done if there will only ever be one of these in the game screen at any time.";
            }

            #endregion

            #region Add Factory to Entity

            mainView.AddEntityFactory.Visibility = ToVisibility(
                selectedEntity != null &&
                selectedEntity.CreatedByOtherEntities == false
                );

            if(mainView.AddEntityFactory.Visibility == Visibility.Visible)
            {
                mainView.AddEntityFactory.Title =
                    $"Add {selectedEntity.GetStrippedName()} Factory";
            }

            #endregion

            foreach(var child in mainView.MainStackPanel.Children)
            {
                if(child is GroupBox groupBox)
                {
                    var wrapPanel = groupBox.Content as WrapPanel;

                    groupBox.Visibility = wrapPanel.Children.Any(item => item.Visibility == Visibility.Visible).ToVisibility();
                }
            }
        }
    }
}
