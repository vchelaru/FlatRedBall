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
                mainView.AddScreenButton.Details = Localization.Texts.HintNeedAtLeastOneScreen;
            }
            else
            {
                mainView.AddScreenButton.Details = Localization.Texts.HintMostGamesMultipleScreens;
            }

            #endregion

            #region Add Entity

            mainView.AddEntityButton.Visibility = ToVisibility(glueProject != null );

            #endregion

            #region Add Object to Screen/Entity

            mainView.AddObjectToEntityButton.Visibility = ToVisibility(
                GlueState.Self.CurrentEntitySave != null
                );
            mainView.AddObjectToEntityButton.Title = String.Format(Localization.Texts.ObjectAddToX, GlueState.Self.CurrentElement?.GetStrippedName());
            mainView.AddObjectToEntityButton.Details = Localization.Texts.HintCommonEntityTypes;

            mainView.AddObjectToScreenButton.Visibility = ToVisibility(
                GlueState.Self.CurrentScreenSave != null
                );
            mainView.AddObjectToScreenButton.Title = String.Format(Localization.Texts.ObjectAddToX, GlueState.Self.CurrentElement?.GetStrippedName());
            mainView.AddObjectToScreenButton.Details = Localization.Texts.HintCommonEntityTypesRelationships;

            #endregion

            #region Add object to List

            NamedObjectSave nosList = null;

            if(GlueState.Self.CurrentElement != null &&
                GlueState.Self.CurrentNamedObjectSave?.IsList == true)
            {
                nosList = GlueState.Self.CurrentNamedObjectSave;
            }

            if(nosList == null && GlueState.Self.CurrentElement != null &&
                GlueState.Self.CurrentNamedObjectSave != null)
            {
                nosList = GlueState.Self.CurrentElement.NamedObjects
                    .FirstOrDefault(item => item.ContainedObjects.Contains(GlueState.Self.CurrentNamedObjectSave));
            }

            var isListReferencingAbstractEntity = nosList != null && ObjectFinder.Self.GetEntitySave(nosList.SourceClassGenericType)?.AllNamedObjects.Any(item => item.SetByDerived) == true;
            var listType = nosList?.SourceClassGenericType;
            if(listType?.Contains('\\') == true)
            {
                listType = FileManager.RemovePath(listType);
            }

            mainView.AddObjectToListInScreenButton.Visibility = 
                ToVisibility(nosList != null && !isListReferencingAbstractEntity && GlueState.Self.CurrentScreenSave != null);
            mainView.AddObjectToListInScreenButton.Title = String.Format(Localization.Texts.AddNewXToY, listType, nosList?.InstanceName);

            mainView.AddObjectToListInEntityButton.Visibility = 
                ToVisibility(nosList != null && !isListReferencingAbstractEntity && GlueState.Self.CurrentElement != null);
            mainView.AddObjectToListInEntityButton.Title = String.Format(Localization.Texts.AddNewXToY, listType, nosList?.InstanceName);


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
                mainView.AddListOfEntityButton.Title = String.Format(Localization.Texts.ListAddXToGameScreen, entityName);

                mainView.AddListOfEntityButton.Details = Localization.Texts.HintListMultipleCopiesOfEntity;
            }

            #endregion

            #region Add Instance of Entity to GameScreen

            var canAddInstanceToGameScreen =
                glueProject != null &&
                selectedObject is EntitySave entitySave &&
                hasGameScreen &&
                // Individual:
                // List:
                //gameScreen.AllNamedObjects.Any(item => item.IsList && item.SourceClassGenericType == (selectedObject as EntitySave).Name) == false
                // Update July 25, 2021 - if the user has a list of an entity, they may still want to add instances to that list, espeically with the 
                // Glue level editor being developed now. So don't exclude this button if a list exists
                // Update Sept 25, 2021
                // Don't allow instances of this if there is a SetByDerived instance
                entitySave.AllNamedObjects.Any(item => item.SetByDerived == true) == false;
            var selectedEntitySave = selectedObject as EntitySave;
            var alreadyHasInstance =
                gameScreen != null && 
                selectedEntitySave != null && 
                gameScreen.AllNamedObjects.Any(item => item.SourceClassType == (selectedEntitySave).Name);


            mainView.AddInstanceOfEntityButton.Visibility = ToVisibility(canAddInstanceToGameScreen);

            if(mainView.AddInstanceOfEntityButton.Visibility == Visibility.Visible)
            {
                var entity = selectedObject as EntitySave;
                var entityName = entity.GetStrippedName();

                string anotherOrEmpty = alreadyHasInstance ? Localization.Texts.Another + " " : String.Empty;

                mainView.AddInstanceOfEntityButton.Title =
                    String.Format(Localization.Texts.GameScreenInstanceAdd, anotherOrEmpty, entityName);

                mainView.AddInstanceOfEntityButton.Details = Localization.Texts.HintGameScreenAddInstance;
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
                    String.Format(Localization.Texts.FactoryAddX, selectedEntity.GetStrippedName());
            }

            #endregion

            //------------------------------------------------------------//

            #region Update GroupBox Visibility

            foreach (var child in mainView.MainStackPanel.Children)
            {
                if(child is GroupBox groupBox)
                {
                    var wrapPanel = groupBox.Content as WrapPanel;

                    groupBox.Visibility = wrapPanel.Children.Any(item => item.Visibility == Visibility.Visible).ToVisibility();
                }
            }

            #endregion
        }
    }
}
