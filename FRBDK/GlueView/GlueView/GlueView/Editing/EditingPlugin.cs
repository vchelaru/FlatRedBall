using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlueView.Plugin;
using System.ComponentModel.Composition;
using EditingControls;
using GlueView.Facades;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Glue.RuntimeObjects;
using FlatRedBall.Glue;

namespace GlueView.Editing
{
    [Export(typeof(GlueViewPlugin))]
    public class EditingPlugin : GlueViewPlugin
    {
        #region Fields
        
        EditingHandles mHandles;

        #endregion

        #region Properties

        public override string FriendlyName
        {
            get { return "Editing Plugin"; }
        }

        public override Version Version
        {
            get { return new Version(); }
        }


        #endregion

        public override void StartUp()
        {
            const bool enabled = false;

            
            mHandles = new EditingHandles();
            mHandles.SelectedObjectChanged += new EventHandler(HandleSelectedObjectChanged);

            if (enabled)
            {
                this.ElementHiglight += new EventHandler(HandleElementHighlight);
                this.Update += new EventHandler(HandleUpdate);
            }
        }

        void HandleSelectedObjectChanged(object sender, EventArgs e)
        {
            ElementRuntime elementRuntime = GlueViewState.Self.HighlightedElementRuntime;

            List<string> variablesToSave = new List<string>() { "X", "Y", "Z" };
            if (elementRuntime is ScalableElementRuntime)
            {
                variablesToSave.Add("ScaleX");
                variablesToSave.Add("ScaleY");
            }

            GlueViewCommands.Self.GlueProjectSaveCommands.UpdateIElementVariables(elementRuntime, variablesToSave);
            GlueViewCommands.Self.GlueProjectSaveCommands.SaveGlux();
        }

        void HandleUpdate(object sender, EventArgs e)
        {
            mHandles.Activity();
        }

        void HandleElementHighlight(object sender, EventArgs e)
        {
            ScalableElementRuntime scalableElementRuntime = GlueViewState.Self.HighlightedElementRuntime as ScalableElementRuntime;

            if (scalableElementRuntime != null)
            {
                mHandles.SelectedObject = scalableElementRuntime.DirectScalableReference;
            }

            else
            {
                mHandles.SelectedObject = GlueViewState.Self.HighlightedElementRuntime;
            }
        }

        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason)
        {
            // internal plugins can't be shut down
            return false;
        }
    }
}
