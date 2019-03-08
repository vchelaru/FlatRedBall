using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
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

        MainEntityView mainView;
        PluginTab tab;

        NetworkEntityViewModel viewModel;

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
        }

        private void CreateUi()
        {
            mainView = new MainEntityView();
            tab = base.CreateTab(mainView, "Network");
        }

        #endregion

        private void HandleItemSelected(TreeNode selectedTreeNode)
        {
            if(selectedTreeNode.IsEntityNode())
            {
                var entity = GlueState.Self.CurrentEntitySave;
                viewModel = new NetworkEntityViewModel();
                viewModel.SetFrom(entity);
                viewModel.PropertyChanged += HandleViewModelPropertyChanged;
                mainView.DataContext = viewModel;


                if (tab.LastTabControl == null)
                {
                    this.ShowTab(tab, TabLocation.Center);
                }
                else
                {
                    this.ShowTab(tab);
                }
            }
            else
            {
                this.RemoveTab(tab);
            }
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var currentEntity = GlueState.Self.CurrentEntitySave;
            if(currentEntity != null && viewModel != null)
            {
                var createdNewVariable = viewModel.ApplyTo(currentEntity);

                if(createdNewVariable)
                {
                    GlueCommands.Self.RefreshCommands.RefreshUiForSelectedElement();
                }

                GlueCommands.Self.GluxCommands.SaveGluxTask();

                NetworkCodeGenerator.GenerateCodeFor(currentEntity);
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }
    }
}
