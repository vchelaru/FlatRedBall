using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using FlatRedBall.Graphics;

using FlatRedBall.Gui;

using FlatRedBall.Math;

#if FRB_XNA
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

#endif

namespace AIEditor.Gui
{
    public class CommandDisplay : PositionedObject
    {
        #region Fields
        Layer mLayer;
        Sprite mCreateLink;
        bool mVisible;
        #endregion

        #region Properties
        public bool Visible
        {
            get { return mVisible; }
            set
            {
                mVisible = value;

                foreach (PositionedObject po in mChildren)
                {
                    if (po is Sprite)
                    {
                        ((Sprite)po).Visible = value;
                    }
                }
            }
        }

        public bool IsCursorOverThis
        {
            get
            {
                // Could cache this off once a frame, but not so concerned with speed right now
                foreach (PositionedObject po in mChildren)
                {
                    if (po is Sprite)
                    {
                        if (GuiManager.Cursor.IsOn(po as Sprite))
                            return true;
                    }
                }

                return false;
            }
        }

        public bool IsCursorOverCreateLinkIcon
        {
            get { return GuiManager.Cursor.IsOn(mCreateLink); }
        }

        #endregion

        #region Methods

        public CommandDisplay()
        {
            mLayer = SpriteManager.AddLayer();

            SpriteManager.AddPositionedObject(this);

            Texture2D texture = FlatRedBallServices.Load<Texture2D>(@"Assets\UI\LinkIcon.png", 
                "Global");

            mCreateLink = SpriteManager.AddSprite(texture, mLayer);

            mCreateLink.ScaleX = mCreateLink.ScaleY = .5f;
            mCreateLink.AttachTo(this, false);
            mCreateLink.RelativeX = -1.5f;
            mCreateLink.RelativeY = 1.5f;

        }

        public override void TimedActivity(float secondDifference, double secondDifferenceSquaredDividedByTwo, float secondsPassedLastFrame)
        {
            base.TimedActivity(secondDifference, secondDifferenceSquaredDividedByTwo, secondsPassedLastFrame);

            float multiplier = 18 / SpriteManager.Camera.PixelsPerUnitAt(this.Z);

            mCreateLink.ScaleX = .5f * multiplier;
            mCreateLink.ScaleY = .5f * multiplier;

            mCreateLink.RelativePosition.X = -1.5f * multiplier;
            mCreateLink.RelativePosition.Y = 1.5f * multiplier;
        }

        #endregion
    }
}
