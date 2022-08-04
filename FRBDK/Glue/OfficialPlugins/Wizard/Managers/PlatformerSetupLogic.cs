using OfficialPluginsCore.Wizard.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.Wizard.Managers
{
    internal static class PlatformerSetupLogic
    {
        public static void SetupForDefaultPlatformer(WizardViewModel viewModel)
        {
            viewModel.PlayerControlType = GameType.Platformer;

            // The FRBBeefcake is SNES resolution so we should make things smaller and zoomed:
            viewModel.SelectedCameraResolution = CameraResolution._480x360;
            viewModel.ScalePercent = 200;

            viewModel.AddPlayerSpritePlatformerAnimations = true;
            viewModel.AddPlatformerAnimationController = true;
        }
    }
}
