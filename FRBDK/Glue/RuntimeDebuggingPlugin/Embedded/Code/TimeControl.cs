using FlatRedBall.Forms.Controls;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.RuntimeDebuggingPlugin.Controls
{
    public class TimeControl : UserControl
    {
        public TimeControl() : base()
        {
            CreateLayout();
        }

        private void CreateLayout()
        {
            var stackPanel = new GraphicalUiElement();
            stackPanel.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
            stackPanel.Parent = this.Visual;

            var restartButton = new Button();
            restartButton.Visual.Parent = stackPanel;
            restartButton.Text = "Restart Screen";
            restartButton.Click += (not, used) => FlatRedBall.Screens.ScreenManager.CurrentScreen.RestartScreen(reloadContent:true);

            var pauseResumeButton = new Button();
            pauseResumeButton.Visual.Parent = stackPanel;
            pauseResumeButton.Text = "Pause";
            restartButton.Click += (not, used) =>
            {
                var screen = FlatRedBall.Screens.ScreenManager.CurrentScreen;

                // todo - screens should expose pause and unpause props
            };



        }
    }
}
