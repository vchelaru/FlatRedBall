using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math.Geometry;
using FlatRedBall;
using EditorObjects.Hud;

#if FRB_MDX
using System.Drawing;
using Microsoft.DirectX;
#else
using Color = Microsoft.Xna.Framework.Graphics.Color;
using Matrix = Microsoft.Xna.Framework.Matrix;
#endif
namespace AIEditor
{
    public class ReactiveHud
    {
        public ScalableSelector mObjectOverHighlight;

        public ClosestNodeToCursorLine mCursorLine;

        Polygon mLinkHighlight;
        Polygon mCurrentLinkHighlight;

        public ReactiveHud()
        {
            mObjectOverHighlight = new ScalableSelector();
            mCursorLine = new ClosestNodeToCursorLine();

            mLinkHighlight = Polygon.CreateRectangle(1, 1);
            mLinkHighlight.Color = Color.LightBlue;

            mCurrentLinkHighlight = Polygon.CreateRectangle(1, 1);
            mCurrentLinkHighlight.Color = Color.Green;
        }

        public void Activity()
        {
            #region NodeOver logic

            if (EditorData.EditingLogic.NodeOver == null)
            {
                mObjectOverHighlight.Visible = false;
            }
            else
            {
                mObjectOverHighlight.Visible = true;

                float scale = EditorData.NodeNetwork.GetVisibleNodeRadius(
                    SpriteManager.Camera, EditorData.EditingLogic.NodeOver);

                mObjectOverHighlight.UpdateToObject(
                    EditorData.EditingLogic.NodeOver.Position,
                    Matrix.Identity,
                    scale,
                    scale,
                    SpriteManager.Camera);

            }

            #endregion

            #region LinkOver logic

            if (EditorData.EditingLogic.LinkOver == null)
            {
                mLinkHighlight.Visible = false;
            }
            else
            {
                mLinkHighlight.Visible = true;

                mLinkHighlight.Position = (EditorData.EditingLogic.LinkOverParent.Position +
                    EditorData.EditingLogic.LinkOver.NodeLinkingTo.Position) * .5f;

                Polygon polygonToScale = mLinkHighlight;


                ScaleLinkMarker(polygonToScale);
            }

            #endregion

            #region CurrentLink Logic

            if (EditorData.EditingLogic.CurrentLink == null)
            {
                mCurrentLinkHighlight.Visible = false;

            }
            else
            {
                mCurrentLinkHighlight.Visible = true;
                ScaleLinkMarker(mCurrentLinkHighlight);
                mCurrentLinkHighlight.Position = (EditorData.EditingLogic.CurrentLinkParent.Position +
                    EditorData.EditingLogic.CurrentLink.NodeLinkingTo.Position) * .5f;
            }



            #endregion

            mCursorLine.Activity(EditorData.NodeNetwork);

        }

        private static void ScaleLinkMarker(Polygon polygonToScale)
        {
            float scale = (float)polygonToScale.Points[0].X;

            float desiredScale = 8 / SpriteManager.Camera.PixelsPerUnitAt(0);

            if (desiredScale != scale)
            {
                polygonToScale.ScaleBy(desiredScale / scale);
            }
        }
    }
}
