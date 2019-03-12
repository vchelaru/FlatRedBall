using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using RedGrinPlugin.CodeGenerators;
using RedGrinPlugin.ViewModels;
using RedGrinPlugin.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RedGrinPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        #region Properties

        public override string FriendlyName
        {
            get { return "RedGrin Networking Plugin"; }
        }

        public override Version Version
        {
            get
            {
                return new Version(1, 0, 0);
            }
        }

        MainEntityView mainEntityView;
        PluginTab entityTab;
        NetworkEntityViewModel entityViewModel;

        MainScreenView mainScreenView;
        PluginTab screenTab;
        NetworkScreenViewModel screenViewModel;

        #endregion

        #region Start

        public override void StartUp()
        {
            AssignEvents();

            CreateUi();
        }

        private void AssignEvents()
        {
            base.ReactToItemSelectHandler += HandleItemSelected;
            base.ReactToChangedPropertyHandler += HandlePropertyChanged;
            this.ReactToVariableRemoved += HandleVariableRemoved;
            base.ReactToEntityRemoved += HandleEntityRemoved;
            base.ReactToScreenRemoved += HandleScreenRemoved;
        }

        private void CreateUi()
        {
            mainEntityView = new MainEntityView();
            entityTab = base.CreateTab(mainEntityView, "Network");

            mainScreenView = new MainScreenView();
            screenTab = base.CreateTab(mainScreenView, "Network");
            
        }

        #endregion

        private void HandleItemSelected(TreeNode selectedTreeNode)
        {
            if(selectedTreeNode.IsEntityNode())
            {
                var entity = GlueState.Self.CurrentEntitySave;
                entityViewModel = new NetworkEntityViewModel();
                entityViewModel.SetFrom(entity);
                entityViewModel.PropertyChanged += HandleEntityViewModelPropertyChanged;
                mainEntityView.DataContext = entityViewModel;


                if (entityTab.LastTabControl == null)
                {
                    this.ShowTab(entityTab, TabLocation.Center);
                }
                else
                {
                    this.ShowTab(entityTab);
                }
            }
            else
            {
                this.RemoveTab(entityTab);
            }

            if(selectedTreeNode.IsScreenNode())
            {
                var screen = GlueState.Self.CurrentScreenSave;
                screenViewModel = new NetworkScreenViewModel();
                screenViewModel.SetFrom(screen);
                screenViewModel.PropertyChanged += HandleScreenViewModelPropertyChanged;
                mainScreenView.DataContext = screenViewModel;

                if(screenTab.LastTabControl == null)
                {
                    this.ShowTab(screenTab, TabLocation.Center);
                }
                else
                {
                    this.ShowTab(screenTab);
                }
            }
            else
            {
                this.RemoveTab(screenTab);
            }
        }

        private void HandleEntityViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var currentEntity = GlueState.Self.CurrentEntitySave;
            if(currentEntity != null && entityViewModel != null)
            {
                TaskManager.Self.AddSync(() =>
                {
                    var createdNewVariable = entityViewModel.ApplyTo(currentEntity);

                    if (createdNewVariable)
                    {
                        GlueCommands.Self.RefreshCommands.RefreshUiForSelectedElement();
                    }

                    GlueCommands.Self.GluxCommands.SaveGluxTask();

                    NetworkEntityCodeGenerator.GenerateCodeFor(currentEntity);

                    // Whenever a new entity is modified, we may need to re-generate all screens because screens
                    // have a switch statement for creating entities based on networked entities:
                    var changedIsNetworked = e.PropertyName ==
                        nameof(NetworkEntityViewModel.IsNetworkEntity);
                    if (changedIsNetworked)
                    {
                        if (NetworkEntityViewModel.IsNetworked(currentEntity) == false)
                        {
                            // set this to not be networked, need to remove the files:
                            var networkedFiles = CodeGeneratorCommonLogic.GetAllNetworkFilesFor(currentEntity);

                            foreach (var file in networkedFiles)
                            {
                                CodeGeneratorCommonLogic.RemoveCodeFileFromProject(file);
                            }
                        }
                        else
                        {
                            NetworkConfigurationCodeGenerator.GenerateConfiguration();
                        }
                        NetworkScreenCodeGenerator.GenerateAllNetworkScreenCode();
                    }
                }, "Reacting to networked entity view model change");
            }
        }

        private void HandleScreenViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var currentScreen = GlueState.Self.CurrentScreenSave;
            if(currentScreen != null && screenViewModel != null)
            {
                TaskManager.Self.AddSync(() =>
                {
                    screenViewModel.ApplyTo(currentScreen);

                    GlueCommands.Self.GluxCommands.SaveGluxTask();

                    var shouldRegenerate = true;

                    if (e.PropertyName == nameof(NetworkScreenViewModel.IsNetworkScreen))
                    {
                        if (NetworkScreenViewModel.IsNetworked(currentScreen) == false)
                        {
                            shouldRegenerate = false;

                            CodeGeneratorCommonLogic.RemoveCodeFileFromProject(
                                CodeGeneratorCommonLogic.GetGeneratedElementNetworkFilePathFor(currentScreen));

                            CodeGeneratorCommonLogic.RemoveCodeFileFromProject(
                                CodeGeneratorCommonLogic.GetCustomElementNetworkFilePathFor(currentScreen));

                        }
                        else
                        {
                            NetworkConfigurationCodeGenerator.GenerateConfiguration();
                        }
                    }
                    if (shouldRegenerate)
                    {
                        NetworkScreenCodeGenerator.GenerateCodeFor(currentScreen);
                    }

                }, $"Regenerating network screen {currentScreen}");
            }
        }

        private void HandlePropertyChanged(string changedMember, object oldValue)
        {
            if(changedMember == nameof(EntitySave.CreatedByOtherEntities) && GlueState.Self.CurrentCustomVariable == null)
            {
                var entity = GlueState.Self.CurrentEntitySave;

                var isNetworkEntity = NetworkEntityViewModel.IsNetworked(entity);
                
                if(isNetworkEntity)
                {
                    TaskManager.Self.AddSync( NetworkScreenCodeGenerator.GenerateAllNetworkScreenCode,
                        "Generating all networked screens because of an entity change");
                }
            }
        }

        private void HandleVariableRemoved(CustomVariable variable)
        {
            var currentEntity = GlueState.Self.CurrentEntitySave;

            if(currentEntity != null && 
                NetworkEntityViewModel.IsNetworked(currentEntity) &&
                NetworkEntityViewModel.IsNetworked(variable)
                )
            {
                TaskManager.Self.AddSync(() =>
                    NetworkEntityCodeGenerator.GenerateCodeFor(currentEntity),
                    "Regenerating networked due to variable removed");
            }
        }

        private void HandleEntityRemoved(EntitySave entity, List<string> filesToRemove)
        {
            if(NetworkEntityViewModel.IsNetworked(entity))
            {
                var filePaths = CodeGeneratorCommonLogic.GetAllNetworkFilesFor(entity);
                filesToRemove.AddRange(filePaths.Select(item => item.FullPath));


                // All screens no longer need to handle creating this:
                TaskManager.Self.AddSync(NetworkScreenCodeGenerator.GenerateAllNetworkScreenCode,
                    "Regenerating network screens due to entity removal");
            }
        }

        private void HandleScreenRemoved(ScreenSave screen, List<string> filesToRemove)
        {
            if(NetworkScreenViewModel.IsNetworked(screen))
            {
                filesToRemove.Add(CodeGeneratorCommonLogic.GetGeneratedElementNetworkFilePathFor(screen).FullPath);
                filesToRemove.Add(CodeGeneratorCommonLogic.GetCustomElementNetworkFilePathFor(screen).FullPath);
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }


    }
}
