using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Graphics;
using Microsoft.Xna.Framework;
using FlatRedBall.Gui;
using System.Windows.Forms;
using FlatRedBall.Math;

namespace EditorObjects
{
    public class FormMethods
    {
        // Used to reposition the UI
        public static FlatRedBall.Math.Geometry.Point TopLeftPixel;

        // The border properties
        // were intended to be used
        // to allow a tool to render 
        // but not occupy the entire window.
        // I don't think we're going to use these
        // anymore, but not sure.
        int mTopBorder;
        int mBottomBorder;
        int mLeftBorder;
        int mRightBorder;


        public FormMethods() : this(0,0,0,0)
        {

        }

        public FormMethods(int topBorder, int leftBorder, int bottomBorder, int rightBorder)
        {
            mTopBorder = topBorder;
            mBottomBorder = bottomBorder;
            mLeftBorder = leftBorder;
            mRightBorder = rightBorder;
            
            FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged += new EventHandler(ReactToResizing);
            FlatRedBallServices.Game.Window.AllowUserResizing = true;



            //Renderer.UseRenderTargets = true;
        }

        //public void AllowFileDrop(System.Windows.Forms.DragEventHandler dragEventHandler)
        //{
        //    FlatRedBallServices.Owner.AllowDrop = true;
        //    FlatRedBallServices.Owner.DragEnter += DragEnter;
        //    FlatRedBallServices.Owner.DragDrop += dragEventHandler;
        //}

        void DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) 
                e.Effect = DragDropEffects.Copy;
        }

        void ReactToResizing(object sender, EventArgs e)
        {
            //while (!Renderer.IsInRendering)
            {

                // Get the new client bounds (the area where things will be drawn)
                Rectangle displayRectangle =
                    FlatRedBallServices.Game.Window.ClientBounds;

                displayRectangle.X += mLeftBorder;
                displayRectangle.Width -= mLeftBorder;
                displayRectangle.Width -= mRightBorder;

                displayRectangle.Y += mTopBorder;
                displayRectangle.Height -= mTopBorder;
                displayRectangle.Height -= mBottomBorder;

                // This tests if the user has minimized the window
                if (displayRectangle.Width == 0 || displayRectangle.Height == 0)
                {
                    // The user has minimized the window.  Don't do anything in this case
                    return;
                }

                int newHeight = displayRectangle.Height;
                int newWidth = displayRectangle.Width;

                ReactToResolutionChange(newHeight, newWidth);
            }
        }

        public void ReactToResolutionChange(int newHeight, int newWidth)
        {
            // Do we need to update things?
            bool hasWindowChanged =
                    SpriteManager.Cameras[0].DestinationRectangle.Height != newHeight ||
                    SpriteManager.Cameras[0].DestinationRectangle.Width != newWidth;

            if (hasWindowChanged)
            {
                // Resize the destination rectangle so the camera renders to the full screen
                // You may need to change this code if using a split screen view.


                double unitPerPixel = SpriteManager.Camera.OrthogonalHeight / SpriteManager.Cameras[0].DestinationRectangle.Height;


                while (true)
                {
                    if (Renderer.IsInRendering)
                    {
                        continue;
                    }
                    else
                    {
                        SpriteManager.Cameras[0].DestinationRectangle = new Rectangle(
                            mLeftBorder, mTopBorder, newWidth, newHeight);
                        SpriteManager.Camera.OrthogonalHeight = (float)(newHeight * unitPerPixel);
                        SpriteManager.Camera.OrthogonalWidth = (float)(newWidth * unitPerPixel);
                        SpriteManager.Cameras[0].FieldOfView = MathFunctions.GetAspectRatioForSameSizeAtResolution(newHeight);
                        SpriteManager.Cameras[0].FixAspectRatioYConstant();
                        break;
                    }
                }

            }

            // Shift the UI so it remains in the same position
            // Update - I don't think we need this anymore now that
            // 0,0 is always the top-left
            //GuiManager.ShiftBy(
            //    -(float)(TopLeftPixel.X + SpriteManager.Cameras[0].XEdge),
            //    -(float)(SpriteManager.Cameras[0].YEdge - TopLeftPixel.Y),
            //    false // Don't shift SpriteFrame GUI
            //    );

            // Adjust the top left pixel so that the UI can be accurately
            // repositioned next time this event is raised.
            // No more need to do this given the new UI rendering code
            //TopLeftPixel.X = -SpriteManager.Cameras[0].XEdge;
            //TopLeftPixel.Y = SpriteManager.Cameras[0].YEdge;
        }
    }
}
