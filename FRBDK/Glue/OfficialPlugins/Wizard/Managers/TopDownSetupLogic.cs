using OfficialPluginsCore.Wizard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.Wizard.Managers
{
    internal class TopDownSetupLogic
    {
        public static void SetupForDefaultTopDown(WizardViewModel viewModel)
        {
            viewModel.PlayerControlType = GameType.TopDown;
            viewModel.AddCloudCollision = false;

            viewModel.WithVisualType = WithVisualType.WithVisuals;

            // The FRBBeefcake is SNES resolution so we should make things smaller and zoomed:
            viewModel.SelectedCameraResolution = CameraResolution._480x360;
            viewModel.ScalePercent = 200;

            viewModel.AddPlayerSpriteTopDownAnimations = true;
            viewModel.AddTopDownAnimationController = true;
        }
    }
}
