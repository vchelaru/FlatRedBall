using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Gui;

#if FRB_XNA
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#endif

namespace EditorObjects.Gui
{
    public class SpriteGridPropertyGrid : PropertyGrid<SpriteGrid>
    {
        #region Properties
        public override SpriteGrid SelectedObject
        {
            get
            {
                return base.SelectedObject;
            }
            set
            {
                base.SelectedObject = value;

                Visible = (SelectedObject != null);
            }
        }
        #endregion

        #region Event Methods

        private void RefreshGrid(Window callingWindow)
        {
            mSelectedObject.PopulateGrid();
        }

        private void SetPixelPerfectClick(Window callingWindow)
        {
            if (SelectedObject.GridPlane != SpriteGrid.Plane.XY)
            {
                GuiManager.ShowMessageBox("Can't set pixel perfect because this SpriteGrid doesn't use the X/Y Plane", "Invalid Operation");
            }
            else if (SelectedObject != null && SelectedObject.Blueprint.Texture != null)
            {
                Texture2D texture = SelectedObject.Blueprint.Texture;

                float gridZ = SelectedObject.Blueprint.Z;


                float pixelsPerUnit = SpriteManager.Camera.PixelsPerUnitAt(gridZ);

                float desiredScaleX = .5f * texture.Width / pixelsPerUnit;
                float desiredSpacing = 2 * desiredScaleX;


                // this could potentially make a LOT of Sprites, so we should run a test to make sure the SE isn't going
                // to make too many Sprites.
                float leftBound = Math.Max(SpriteManager.Camera.AbsoluteLeftXEdgeAt(gridZ), SelectedObject.XLeftBound);
                float rightBound = Math.Min(SpriteManager.Camera.AbsoluteRightXEdgeAt(gridZ), SelectedObject.XRightBound);

                float topBound = Math.Min(SpriteManager.Camera.AbsoluteTopYEdgeAt(gridZ), SelectedObject.YTopBound);
                float bottomBound = Math.Max(SpriteManager.Camera.AbsoluteBottomYEdgeAt(gridZ), SelectedObject.YBottomBound);

                float numberX = (rightBound - leftBound) / desiredSpacing;
                float numberY = (topBound - bottomBound) / desiredSpacing;

                int totalNumber = FlatRedBall.Math.MathFunctions.RoundToInt(numberX * numberY);

                if (totalNumber > 10000)
                {
                    OkCancelWindow okCancelWindow = GuiManager.ShowOkCancelWindow("This operation will result in approximately " + totalNumber + " Sprites in view. " +
                        "This could make the SpriteEditor run very slowly or even appear to completely freeze.  Are you sure you want " +
                        "to perform this action?", "Are you sure?");

                    okCancelWindow.OkClick += new GuiMessage(ForceSetToPixelPerfect);
                    okCancelWindow.OkText = "Yes";
                    okCancelWindow.CancelText = "No";
                }
                else
                {
                    ForceSetToPixelPerfect(null);
                }
            }
        }

        private void ForceSetToPixelPerfect(IWindow callingWindow)
        {
            float gridZ = SelectedObject.Blueprint.Z;
            Texture2D texture = SelectedObject.Blueprint.Texture;
            float pixelsPerUnit = SpriteManager.Camera.PixelsPerUnitAt(gridZ);

            SelectedObject.Blueprint.ScaleX = .5f * texture.Width / pixelsPerUnit;
            SelectedObject.Blueprint.ScaleY = .5f * texture.Height / pixelsPerUnit;

            SelectedObject.GridSpacing = 2 * SelectedObject.Blueprint.ScaleX;

            RefreshGrid(null);
        }

        #endregion

        #region Methods
        public SpriteGridPropertyGrid(Cursor cursor)
            : base(cursor)
        {
            ExcludeAllMembers();

            #region Basic category
            IncludeMember("Name", "Basic");
            IncludeMember("GridSpacing", "Basic");
            SetMemberChangeEvent("GridSpacing", RefreshGrid);

            UpDown upDown = GetUIElementForMember("GridSpacing") as UpDown;
            upDown.MinValue = .2f; // This is to prevent the user from freezing the SpriteEditor.  May need to make this
            // a little more robust later.  Will do the trick for now.

            Button setPixelPerfect = new Button(mCursor);
            setPixelPerfect.ScaleX = 5;
            setPixelPerfect.ScaleY = 2;
            setPixelPerfect.Text = "Set Pixel\nPerfect";
            AddWindow(setPixelPerfect, "Basic");
            setPixelPerfect.Click += SetPixelPerfectClick;

            #endregion

            #region Sprite Creation

            IncludeMember("OrderingMode", "Sprite Creation");
            SetMemberChangeEvent("OrderingMode", RefreshGrid);
            ExcludeEnumerationValue("OrderingMode", "Undefined");

            IncludeMember("CreatesAutomaticallyUpdatedSprites", "Sprite Creation");
            SetMemberChangeEvent("CreatesAutomaticallyUpdatedSprites", RefreshGrid);

            IncludeMember("CreatesParticleSprites", "Sprite Creation");
            SetMemberChangeEvent("CreatesParticleSprites", RefreshGrid);


            #endregion

            #region Bounds category

            IncludeMember("XLeftBound", "Bounds");
            IncludeMember("XRightBound", "Bounds");
            IncludeMember("YTopBound", "Bounds");
            IncludeMember("YBottomBound", "Bounds");
            IncludeMember("ZCloseBound", "Bounds");
            IncludeMember("ZFarBound", "Bounds");

            #endregion

            /*
            IncludeMember("RelativeX", "Relative");
            IncludeMember("RelativeY", "Relative");
            IncludeMember("RelativeZ", "Relative");

            IncludeMember("RelativeRotationX", "Relative");
            IncludeMember("RelativeRotationY", "Relative");
            IncludeMember("RelativeRotationZ", "Relative");


            IncludeMember("ColorOperation", "Color");
            IncludeMember("Red",            "Color");
            IncludeMember("Green",          "Color");
            IncludeMember("Blue",           "Color");

            IncludeMember("BlendOperation", "Blend");
            IncludeMember("Alpha",          "Blend");

*/


            RemoveCategory("Uncategorized");

            SelectCategory("Basic");
        }
        #endregion

    }
}
