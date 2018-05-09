using FlatRedBall.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RuntimeDebugProj.GumRuntimes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.RuntimeDebuggingPlugin.Controls
{
    public class TimeControl : UserControl
    {
        float borderWidth = 5;

        public TimeControl() : base()
        {
            CreateLayout();
        }

        private void CreateLayout()
        {

            this.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.Width = borderWidth;
            this.Height = borderWidth;

            var stackPanel = new ContainerRuntime();
            stackPanel.ExposeChildrenEvents = true;
            stackPanel.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
            stackPanel.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            stackPanel.Width = 0;
            stackPanel.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            stackPanel.Height = 0;
            stackPanel.Parent = this.Visual;

            var iconSpriteSheet = FlatRedBallServices.Load<Texture2D>("content/globalContent/runtimedebugging/spritesheet.png");

            CreateRestartButton(stackPanel, iconSpriteSheet);

            CreatePauseResumeButton(stackPanel, iconSpriteSheet);

        }

        private void CreatePauseResumeButton(ContainerRuntime stackPanel, Texture2D iconSpriteSheet)
        {
            var pauseResumeButton = new Button();

            var internalSprite = new global::RenderingLibrary.Graphics.Sprite(iconSpriteSheet);
            var sprite = new GraphicalUiElement(internalSprite, null);
            sprite.Parent = pauseResumeButton.Visual;
            InitializeIconSprite(sprite);

            SetToPauseIcon(sprite);

            pauseResumeButton.Visual.Parent = stackPanel;
            pauseResumeButton.Text = "";
            // do an absolute width because otherwise it will change size according to the icons
            pauseResumeButton.Visual.Width = 36;
            pauseResumeButton.Visual.Height = 38;
            pauseResumeButton.X = borderWidth;
            pauseResumeButton.Y = borderWidth;
            pauseResumeButton.Push += (not, used) =>
            {

                var screen = FlatRedBall.Screens.ScreenManager.CurrentScreen;
                if (screen.IsPaused)
                {
                    screen.UnpauseThisScreen();
                    SetToPauseIcon(sprite);
                }
                else
                {
                    screen.PauseThisScreen();
                    SetToPlayIcon(sprite);
                }
            };
        }

        private void CreateRestartButton(ContainerRuntime stackPanel, Texture2D iconSpriteSheet)
        {
            var restartButton = new Button();
            restartButton.Visual.Parent = stackPanel;
            restartButton.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            restartButton.Visual.Width = 10;
            restartButton.Visual.Height = 38;
            restartButton.X = borderWidth;
            restartButton.Y = borderWidth;
            restartButton.Text = "";
            restartButton.Push += (not, used) => FlatRedBall.Screens.ScreenManager.CurrentScreen.RestartScreen(reloadContent: true);

            var internalSprite = new global::RenderingLibrary.Graphics.Sprite(iconSpriteSheet);
            var restartSprite = new GraphicalUiElement(internalSprite, null);
            restartSprite.Parent = restartButton.Visual;
            InitializeIconSprite(restartSprite);
            restartSprite.TextureLeft = 0;
            restartSprite.TextureTop = 0;
            restartSprite.TextureWidth = 30;
            restartSprite.TextureHeight = 30;
        }

        private static void InitializeIconSprite(GraphicalUiElement sprite)
        {
            sprite.Width = 100;
            sprite.Height = 100;
            sprite.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            sprite.HeightUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            sprite.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            sprite.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
            sprite.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            sprite.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            sprite.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
        }

        private static void SetToPlayIcon(GraphicalUiElement sprite)
        {
            sprite.TextureLeft = 27;
            sprite.TextureTop = 0;
            sprite.TextureWidth = 22;
            sprite.TextureHeight = 24;
        }

        private static void SetToPauseIcon(GraphicalUiElement sprite)
        {
            sprite.TextureLeft = 47;
            sprite.TextureTop = 0;
            sprite.TextureWidth = 16;
            sprite.TextureHeight = 22;
        }

    }
}
