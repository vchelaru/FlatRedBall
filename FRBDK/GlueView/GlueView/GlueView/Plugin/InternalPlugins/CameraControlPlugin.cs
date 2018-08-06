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

            FlatRedBall.Camera.Main.SetSplitScreenViewport(null);// we're gonna handle this manually

            FlatRedBall.FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged += HandleSizeOrOrientationChanged;
        }

        private void HandleSizeOrOrientationChanged(object sender, EventArgs e)
        {

            FlatRedBall.Camera.Main.DestinationRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0,
                FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth,
                FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight);

            var displaySettings = GlueViewState.Self.CurrentGlueProject?.DisplaySettings;

            if(displaySettings != null && displaySettings.Is2D)
            {
                if(displaySettings.FixedAspectRatio)
                {
                    var displaySettingsAspectRatio = (float)
                        (displaySettings.AspectRatioWidth / displaySettings.AspectRatioHeight);

                    var canvasAspectRatio = (float)FlatRedBall.Camera.Main.DestinationRectangle.Width /
                        FlatRedBall.Camera.Main.DestinationRectangle.Height;

                    if (canvasAspectRatio > displaySettingsAspectRatio)
                    {
                        FlatRedBall.Camera.Main.OrthogonalHeight =
                            displaySettings.ResolutionHeight;

                        FlatRedBall.Camera.Main.FixAspectRatioYConstant();
                    }
                    else
                    {
                        FlatRedBall.Camera.Main.OrthogonalWidth =
                            displaySettings.ResolutionWidth;

                        FlatRedBall.Camera.Main.FixAspectRatioXConstant();
                    }
                }
                else
                {
                    FlatRedBall.Camera.Main.OrthogonalHeight =
                        displaySettings.ResolutionHeight;

                    FlatRedBall.Camera.Main.FixAspectRatioYConstant();
                }
            }
            
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
