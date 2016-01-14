using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Graphics;
using FlatRedBall;
using FlatRedBall.Math;

#if FRB_MDX

using Microsoft.DirectX.Direct3D;

#endif

namespace EditorObjects.Gui
{
    public class TextPropertyGrid : PropertyGrid<Text>
    {
        #region Fields

        Button mSetPixelPerfectScaleButton;

        public static PositionedObjectList<Camera> ExtraCamerasForScale = new PositionedObjectList<Camera>();
        #endregion

        #region Properties



        public override Text SelectedObject
        {
            get
            {
                return base.SelectedObject;
            }
            set
            {
                base.SelectedObject = value;

                if (!Visible && (SelectedObject != null))
                {
                    GuiManager.BringToFront(this);
                }

                Visible = (SelectedObject != null);


            }
        }

        public string SetPixelPerfectText
        {
            get
            {
                return mSetPixelPerfectScaleButton.Text;
            }
            set
            {
                mSetPixelPerfectScaleButton.Text = value;
            }
        }

        #endregion

        #region Event Methods

        private void SetPixelPerfectScaleClick(Window callingWindow)
        {

            OkListWindow okListWindow = new OkListWindow("Which camera would you like to scale according to?", "Select Camera");

            foreach (Camera camera in SpriteManager.Cameras)
            {
                okListWindow.AddItem(camera.Name, camera);
            }

            foreach (Camera camera in ExtraCamerasForScale)
            {
                okListWindow.AddItem(camera.Name, camera);
            }


            okListWindow.OkButtonClick += SetPixelPerfectScaleOk;
        }

        private void SetPixelPerfectScaleOk(Window callingWindow)
        {
            if (SelectedObject != null)
            {
                OkListWindow okListWindow = callingWindow as OkListWindow;

                Camera camera = okListWindow.GetFirstHighlightedObject() as Camera;

                if (camera == null)
                {
                    GuiManager.ShowMessageBox("No Camera was selected, so Scale has not changed", "No Camera");
                }
                else
                {
                    SelectedObject.SetPixelPerfectScale(camera);
                }
            }


        }

        private void FontChanged(Window callingWindow)
        {
            BitmapFontChangePropertyGrid propertyGrid = (BitmapFontChangePropertyGrid)callingWindow;

            BitmapFont oldFont = propertyGrid.LastBitmapFont;
            BitmapFont newFont = SelectedObject.Font;

            if (oldFont != null && newFont != null)
            {
                float scaleAmount = newFont.LineHeightInPixels / (float)oldFont.LineHeightInPixels;

                SelectedObject.Scale *= scaleAmount;
                SelectedObject.Spacing *= scaleAmount;
                SelectedObject.NewLineDistance *= scaleAmount;

                // Do we want to do anything here?
                //if (SelectedObject.AdjustPositionForPixelPerfectDrawing)
                //{
                //    NewLineDistance = (float)System.Math.Round(Scale * 1.5f);
                //}
                //else
                //{
                //    NewLineDistance = Scale * 1.5f;
                //}
            }

        }

        #endregion

        #region Methods

        public TextPropertyGrid(Cursor cursor)
            : base(cursor)
        {
            MinimumScaleY = 8;

           // GuiManager.AddWindow(this);

            this.ExcludeAllMembers();

            #region Basic

            this.IncludeMember("X", "Basic");
            this.IncludeMember("Y", "Basic");
            this.IncludeMember("Z", "Basic");

            this.IncludeMember("RotationZ", "Basic");

            this.IncludeMember("Visible", "Basic");

            this.IncludeMember("DisplayText", "Basic");

            this.IncludeMember("Name", "Basic");

            #endregion

            #region Alignment

            this.IncludeMember("HorizontalAlignment", "Alignment");
            this.IncludeMember("VerticalAlignment", "Alignment");

            #endregion

            #region Color

            this.IncludeMember("Red", "Color");
            this.IncludeMember("Green", "Color");
            this.IncludeMember("Blue", "Color");
            this.IncludeMember("ColorOperation", "Color");
#if !FRB_XNA
            ComboBox colorOperationComboBox = GetUIElementForMember("ColorOperation") as ComboBox;

            for (int i = colorOperationComboBox.Count - 1; i > -1; i--)
            {
                TextureOperation textureOperation =
                    ((TextureOperation)colorOperationComboBox[i].ReferenceObject);

                if (!FlatRedBall.Graphics.GraphicalEnumerations.IsTextureOperationSupportedInFrbXna(
                    textureOperation))
                {
                    colorOperationComboBox.RemoveAt(i);
                }
            }
#endif

            this.IncludeMember("Alpha", "Color");
            this.IncludeMember("BlendOperation", "Color");

            #endregion

            #region Scale
            this.IncludeMember("Scale", "Scale");
            this.IncludeMember("Spacing", "Scale");
            this.IncludeMember("NewLineDistance", "Scale");

            mSetPixelPerfectScaleButton = new Button(cursor);
            mSetPixelPerfectScaleButton.Text = "Set Pixel-Perfect\nScale";
            mSetPixelPerfectScaleButton.ScaleX = 7.3f;
            mSetPixelPerfectScaleButton.ScaleY = 2;

            AddWindow(mSetPixelPerfectScaleButton, "Scale");
            mSetPixelPerfectScaleButton.Click += SetPixelPerfectScaleClick;

            this.IncludeMember("MaxWidth", "Scale");
            this.IncludeMember("MaxWidthBehavior", "Scale");


            #endregion

            this.IncludeMember("Font", "Font");
            this.SetMemberChangeEvent("Font", FontChanged);

            this.RemoveCategory("Uncategorized");
        }

        #endregion
    }
}
