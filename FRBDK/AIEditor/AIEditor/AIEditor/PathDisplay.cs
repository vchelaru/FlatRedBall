using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using FlatRedBall.AI.Pathfinding;

using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;

using FlatRedBall.Graphics;

#if FRB_XNA
using Color = Microsoft.Xna.Framework.Graphics.Color;
#endif

namespace AIEditor
{
    #region XML Docs
    /// <summary>
    /// Displays the real-time cost of a path whenever a node is selected using Text objects.
    /// </summary>
    #endregion
    public class PathDisplay
    {
        #region Fields

        private PositionedObjectList<Line> mPath = new PositionedObjectList<Line>();

        private PositionedObjectList<Text> mCosts = new PositionedObjectList<Text>();

        #endregion

        #region Methods

        public void ClearPath()
        {
            while (mPath.Count != 0)
            {
                ShapeManager.Remove(mPath[0]);
            }

            while (mCosts.Count != 0)
            {
                TextManager.RemoveText(mCosts[0]);
            }
        }

        public void ShowPath(List<PositionedNode> path)
        {
            if (path.Count == 0)
            {
                return;
            }

            Text text;

            for (int i = 0; i < path.Count - 1; i++)
            {
                Line line = new Line();
                line.Visible = true;
                line.RelativePoint1.X = 0;
                line.RelativePoint1.Y = 0;

                line.Position = path[i].Position;

                line.RelativePoint2.X = path[i + 1].X - line.Position.X;
                line.RelativePoint2.Y = path[i + 1].Y - line.Position.Y;
                line.Color = Color.Yellow;

                mPath.Add(line);

                text = TextManager.AddText(path[i].CostToGetHere.ToString());
                text.Position = path[i].Position;
                text.X += .5f;
                text.Y += .5f;
                mCosts.Add(text);
            }

            text = TextManager.AddText(path[path.Count - 1].CostToGetHere.ToString());
            text.Position = path[path.Count - 1].Position;
            text.X += .5f;
            text.Y += .5f;
            mCosts.Add(text);
        }

        #endregion
    }
}
