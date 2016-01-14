using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Math;

using FlatRedBall.Graphics;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Input;
using FlatRedBall.AI.Pathfinding;

#if FRB_MDX
using Microsoft.DirectX;
using Color = System.Drawing.Color;
using Keys = Microsoft.DirectX.DirectInput.Key;
#else
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Color = Microsoft.Xna.Framework.Graphics.Color;
using Keys = Microsoft.Xna.Framework.Input.Keys;
#endif

namespace EditorObjects.Hud
{
    public class ClosestNodeToCursorLine : Line
    {

        private PositionedNode mNode;

        private Vector3 mMousePosition = new Vector3(0, 0, 1);

        public ClosestNodeToCursorLine() : base()
        {
            ShapeManager.AddLine(this);
            this.Color = Color.Fuchsia;
            this.RelativePoint1.Z = 1;
            this.RelativePoint2.Z = 1;
            this.Visible = false;
        }

        public void Activity(NodeNetwork nodeNetwork)
        {
            //If SHIFT+C is down, make the line visible and redraw it, otherwise make it invisible.
            if (InputManager.Keyboard.KeyDown(Keys.C) &&
                (InputManager.Keyboard.KeyDown(Keys.LeftShift) ||
                (InputManager.Keyboard.KeyDown(Keys.RightShift))))
            {
                this.RelativePoint1.X = mMousePosition.X = InputManager.Mouse.WorldXAt(1);
                this.RelativePoint1.Y = mMousePosition.Y = InputManager.Mouse.WorldYAt(1);
                mNode = nodeNetwork.GetClosestNodeTo(ref mMousePosition);
                if (mNode != null)
                {
                    this.RelativePoint2.X = mNode.Position.X;
                    this.RelativePoint2.Y = mNode.Position.Y;
                    this.Visible = true;
                }
            }
            else
            {
                this.Visible = false;
            }
        }
    }
}
