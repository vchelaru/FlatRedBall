using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Math.Geometry;
using EditingControls;
using FlatRedBall.Graphics;
using FlatRedBall;
using Microsoft.Xna.Framework;
using FlatRedBall.Gui;

namespace EditingControlsViewProject.Screens
{
    public class ObjectContainerScreen : FlatRedBall.Screens.Screen
    {
        AxisAlignedRectangle mRectangle;
        Sprite mSprite;

        ObjectHighlight mObjectHighlight;
        EditingHandles mEditingHandles;

        public ObjectContainerScreen()
            : base("ObjectContainerScreen")
        {

        }

        public override void Initialize(bool addToManagers)
        {
            base.Initialize(addToManagers);

            mObjectHighlight = new ObjectHighlight();
            mObjectHighlight.Color = Color.Orange;
            mEditingHandles = new EditingHandles();

            mRectangle = ShapeManager.AddAxisAlignedRectangle();
            mSprite = SpriteManager.AddSprite("redball.bmp");
            mSprite.X = 5;
        }

        public override void Activity(bool firstTimeCalled)
        {
            base.Activity(firstTimeCalled);

            mEditingHandles.Activity();
            mObjectHighlight.Activity();


            Cursor cursor = GuiManager.Cursor;

            if (cursor.PrimaryClick && mEditingHandles.IsOnHandles == false)
            {
                if (cursor.IsOn3D(mSprite))
                {
                    mObjectHighlight.HighlightedObject = mSprite;
                    mEditingHandles.SelectedObject = mSprite;
                }

                if (cursor.IsOn3D(mRectangle))
                {
                    mObjectHighlight.HighlightedObject = mRectangle;
                    mEditingHandles.SelectedObject = mRectangle;
                }
            }




        }
    }
}
