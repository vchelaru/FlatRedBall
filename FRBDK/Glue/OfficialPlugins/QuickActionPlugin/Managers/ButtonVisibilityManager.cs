using FlatRedBall.Glue.Elements;
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
using ToolsUtilities;

namespace OfficialPluginsCore.QuickActionPlugin.Managers
{
    class ButtonVisibilityManager
    {
        #region Fields/Properties
        
        MainView mainView;

        #endregion

        public ButtonVisibilityManager(MainView mainView)
        {
            this.mainView = mainView;
        }

        public void UpdateVisibility(bool forceUnloaded = false)
        {
            #region Helper Methods

            Visibility ToVisibility(bool value)
            {
                if (value) return Visibility.Visible;
                else return Visibility.Collapsed;
            }

            #endregion

            #region Get variables needed for the rest of the method...

            var glueProject = forceUnloaded ? null : GlueState.Self.CurrentGlueProject;
            var treeNode = GlueState.Self.CurrentTreeNode;
            var selectedObject = treeNode?.Tag;
            var selectedElement = selectedObject as IElement;
            var selectedEntity = selectedObject as EntitySave;

            var gameScreen = glueProject?.Screens.FirstOrDefault(item => item.Name == "Screens\\GameScreen");
            var hasGameScreen = gameScreen != null;
            var hasGumProject = GlueState.Self.GetAllReferencedFiles()
                .Any(item => item.Name.ToLowerInvariant().EndsWith(".gumx"));

            #endregion

            //------------------------------------------------------------//

            #region Wizard 

            var isWizardButtonVisible =
                glueProject != null &&
                glueProject.Screens.Count == 0 &&
                glueProject.Entities.Count == 0 &&
                glueProject.GlobalFiles.Count == 0;

            mainView.RunWizardButton.Visibility = ToVisibility(isWizardButtonVisible);

            #endregion

            #region Create New Project

            mainView.CreateNewProjectButton.Visibility = ToVisibility(
                glueProject == null);

            #endregion

            #region Open Project

            mainView.OpenProjectButton.Visibility = ToVisibility(
                glueProject == null);

            #endregion

            #region Add Gum Project

            mainView.AddGumProject.Visibility = ToVisibility(!hasGumProject && glueProject != null);

            #endregion

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

            #region Add Object to Screen/Entity/List

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

            NamedObjectSave nosList = null;

            if(GlueState.Self.CurrentScreenSave != null &&
                GlueState.Self.CurrentNamedObjectSave?.IsList == true)
            {
                nosList = GlueState.Self.CurrentNamedObjectSave;
            }
            if(nosList == null && GlueState.Self.CurrentScreenSave != null &&
                GlueState.Self.CurrentNamedObjectSave != null)
            {
                nosList = GlueState.Self.CurrentElement.NamedObjects
                    .FirstOrDefault(item => item.ContainedObjects.Contains(GlueState.Self.CurrentNamedObjectSave));
            }

            var isListReferencingAbstractEntity = nosList != null && ObjectFinder.Self.GetEntitySave(nosList.SourceClassGenericType)?.AllNamedObjects.Any(item => item.SetByDerived) == true;

            mainView.AddObjectToListButton.Visibility = ToVisibility(nosList != null && !isListReferencingAbstractEntity);
            var listType = nosList?.SourceClassGenericType;
            if(listType?.Contains('\\') == true)
            {
                listType = FileManager.RemovePath(listType);
            }

            mainView.AddObjectToListButton.Title = $"Add a new {listType} to {nosList?.InstanceName}";


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
                selectedObject is EntitySave entitySave &&
                hasGameScreen &&
                // Individual:
                gameScreen.AllNamedObjects.Any(item => item.SourceClassType == (selectedObject as EntitySave).Name) == false &&
                // List:
                //gameScreen.AllNamedObjects.Any(item => item.IsList && item.SourceClassGenericType == (selectedObject as EntitySave).Name) == false
                // Update July 25, 2021 - if the user has a list of an entity, they may still want to add instances to that list, espeically with the 
                // Glue level editor being developed now. So don't exclude this button if a list exists
                // Update Sept 25, 2021
                // Don't allow instances of this if there is a SetByDerived instance
                entitySave.AllNamedObjects.Any(item => item.SetByDerived == true) == false
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
                selectedEntity.CreatedByOtherEntities == false &&
                selectedEntity.AllNamedObjects.Any(item => item.SetByDerived) == false
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
