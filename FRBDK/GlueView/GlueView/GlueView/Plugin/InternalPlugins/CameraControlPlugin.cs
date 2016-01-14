using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using GlueView.Forms;
using GlueView.Facades;
using FlatRedBall.Glue.SaveClasses;

namespace GlueView.Plugin.InternalPlugins
{
    [Export(typeof(GlueViewPlugin))]
    public class CameraControlPlugin : EmbeddedPlugin
    {
        public override void StartUp()
        {
            this.ElementLoaded += new EventHandler(HandleElementLoaded);


        }

        void HandleElementLoaded(object sender, EventArgs e)
        {
            bool isCameraHandledByCurrentElement = false;

            // Glue camera settings
            foreach (var nos in GlueViewState.Self.CurrentElement.GetAllNamedObjectsRecurisvely())
            {
                if (nos.SourceType == SourceType.FlatRedBallType & nos.SourceClassType == "Camera" &&
                    !nos.IsNewCamera)
                {
                    isCameraHandledByCurrentElement = true;
                    break;
                }
            }

            if (!isCameraHandledByCurrentElement)
            {
                CameraControl.Self.RefreshCamera();
            }
        }
    }
}
