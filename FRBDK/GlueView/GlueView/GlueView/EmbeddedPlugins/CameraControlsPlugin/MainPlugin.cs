using GlueView.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;
using GlueView.Facades;
using GlueView.Forms;
using GlueView.EmbeddedPlugins.CameraControlsPlugin.ViewModels;
using System.ComponentModel;

namespace GlueView.EmbeddedPlugins.CameraControlsPlugin
{
    [Export(typeof(GlueViewPlugin))]
    public class MainPlugin : GlueViewPlugin
    {
        #region Fields/Properties

        BoundsLogic boundsLogic;
        GuidesViewModel guidesViewModel;

        public override string FriendlyName
        {
            get
            {
                return "Camera Plugin";
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(1, 0);
            }
        }

        #endregion

        public override void StartUp()
        {
            boundsLogic = new CameraControlsPlugin.BoundsLogic();

            guidesViewModel = new GuidesViewModel();
            guidesViewModel.PropertyChanged += HandleGuidesPropertyChanged;

            guidesViewModel.CellSize = 20;

            GlueViewCommands.Self.CollapsibleFormCommands.AddCollapsableForm(
                "Camera", -1, new CameraControl(guidesViewModel), this);

            this.ElementLoaded += HandleElementLoaded;
        }

        private void HandleGuidesPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(GuidesViewModel.ShowOrigin):
                    boundsLogic.ShowOrigin = guidesViewModel.ShowOrigin;
                    break;
                case nameof(GuidesViewModel.ShowGrid):
                    boundsLogic.ShowGrid = guidesViewModel.ShowGrid;
                    break;
                case nameof(GuidesViewModel.CellSize):
                    boundsLogic.CellSize = guidesViewModel.CellSize;
                    break;
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        private void HandleElementLoaded(object sender, EventArgs e)
        {
            boundsLogic.HandleElementLoaded();
        }
    }
}
