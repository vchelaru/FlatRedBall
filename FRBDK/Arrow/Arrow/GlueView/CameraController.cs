using FlatRedBall.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Arrow.GlueView
{
    public class CameraController
    {

        public void Activity()
        {
            Cursor frbCursor = GuiManager.Cursor;

            if (frbCursor.MiddleDown)
            {
                float zToMoveAt = 0;

                float xMovement = frbCursor.WorldXChangeAt(zToMoveAt);
                float yMovement = frbCursor.WorldYChangeAt(zToMoveAt);

                SpriteManager.Camera.X -= xMovement;
                SpriteManager.Camera.Y -= yMovement;
            }
        }

    }
}
