using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using FlatRedBall.SpecializedXnaControls.RegionSelection;
using FlatRedBall.AnimationEditorForms.Textures;
using FlatRedBall.AnimationEditorForms.Controls;

namespace FlatRedBall.AnimationEditorForms
{
    public partial class TextureCoordinateSelectionWindow : UserControl
    {

        // This causes problems if it's initialized in the winforms designer, so
        // we do it here in code.
        private SpecializedXnaControls.ImageRegionSelectionControl imageRegionSelectionControl1;

        public InspectableTexture InspectableTexture
        {
            get;
            private set;
        } = new InspectableTexture();

        public new event Action RegionChanged;
        public event Action EndRegionChanged;

        #region Properties

        public Texture2D CurrentTexture
        {
            get
            {
                return imageRegionSelectionControl1.CurrentTexture;
            }
        }


        public RectangleSelector RectangleSelector
        {
            get
            {
                return imageRegionSelectionControl1.RectangleSelector;
            }
        }

        #endregion

        public TextureCoordinateSelectionWindow()
        {
            InitializeComponent();

            InstantiateImageRegionSelectionControl();

            this.imageRegionSelectionControl1.AvailableZoomLevels = this.wireframeEditControls1.AvailableZoomLevels;

        }

        private void InstantiateImageRegionSelectionControl()
        {
            this.imageRegionSelectionControl1 = new FlatRedBall.SpecializedXnaControls.ImageRegionSelectionControl();

            this.imageRegionSelectionControl1.CurrentTexture = null;
            this.imageRegionSelectionControl1.DesiredFramesPerSecond = 10;
            this.imageRegionSelectionControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            //this.imageRegionSelectionControl1.Width = 100;
            //this.imageRegionSelectionControl1.Height = 100;
            //this.imageRegionSelectionControl1.Location = new System.Drawing.Point(0, 23);
            this.imageRegionSelectionControl1.Name = "imageRegionSelectionControl1";
            //this.imageRegionSelectionControl1.Size = new System.Drawing.Size(296, 264);
            this.imageRegionSelectionControl1.TabIndex = 0;
            this.imageRegionSelectionControl1.Text = "imageRegionSelectionControl1";
            this.Controls.Add(this.imageRegionSelectionControl1);
            this.imageRegionSelectionControl1.BringToFront();
            this.imageRegionSelectionControl1.RoundRectangleSelectorToUnit = true;

            this.imageRegionSelectionControl1.Click += HandleClick;
            this.imageRegionSelectionControl1.MouseWheelZoom += HandleMouseWheelZoom;

            this.imageRegionSelectionControl1.RegionChanged += HandleRegionChanged;
            this.imageRegionSelectionControl1.EndRegionChanged += HandleEndRegionChanged;
            this.imageRegionSelectionControl1.SystemManagers.Renderer.Camera.CameraCenterOnScreen =
                RenderingLibrary.CameraCenterOnScreen.TopLeft;
        }




        private void HandleClick(object sender, EventArgs e)
        {
            if (e is MouseEventArgs && ((MouseEventArgs)e).Button == MouseButtons.Left)
            {
                var cursor = imageRegionSelectionControl1.XnaCursor;

                bool shouldApplyMagicWandClick =
                    
                    this.wireframeEditControls1.DataContext?.IsMagicWandSelected == true && 
                    cursor.IsInWindow && 
                    imageRegionSelectionControl1.CurrentTexture != null;

                if (shouldApplyMagicWandClick)
                {
                    HandleMagicWandClick(cursor);
                }
            }
        }

        private void HandleMagicWandClick(InputLibrary.Cursor cursor)
        {
            int minX;
            int minY;
            int maxX;
            int maxY;

            var camera = imageRegionSelectionControl1.SystemManagers.Renderer.Camera;

            float worldX;
            float worldY;
            camera.ScreenToWorld(cursor.X, cursor.Y, out worldX, out worldY); ;

            InspectableTexture.GetOpaqueWandBounds((int)worldX, (int)worldY, out minX, out minY, out maxX, out maxY);

            bool hasValidSelection = minX > 0 && maxX > 0;

            if (hasValidSelection)
            {
                var texture = imageRegionSelectionControl1.CurrentTexture;
                SetSelectionTextureCoordinates(minY, maxY , minX , maxX );

                EndRegionChanged?.Invoke();
            }
        }

        private void HandleRegionChanged(object sender, EventArgs e)
        {
            RegionChanged?.Invoke();
        }

        private void HandleEndRegionChanged(object sender, EventArgs e)
        {
            EndRegionChanged?.Invoke();
        }

        public void ShowSprite(string fullFileName, float topTexture, float bottomTexture, float leftTexture, float rightTexture)
        {
            this.imageRegionSelectionControl1.SystemManagers.Renderer.Camera.X = -8;
            this.imageRegionSelectionControl1.SystemManagers.Renderer.Camera.Y = -8;

            bool isSameFile = false;
            Texture2D texture = null;
            if (!string.IsNullOrEmpty(fullFileName) && System.IO.File.Exists(fullFileName))
            {
                var fileNameStandardized = ToolsUtilities.FileManager.Standardize(fullFileName);

                isSameFile = fileNameStandardized == imageRegionSelectionControl1?.CurrentTexture?.Name;

                if(!isSameFile)
                {
                    texture = LoaderManager.Self.LoadContent<Texture2D>(fullFileName);
                }
            }
            else
            {
                isSameFile = imageRegionSelectionControl1?.CurrentTexture == null;
            }
            if(!isSameFile)
            {
                if (texture != imageRegionSelectionControl1.CurrentTexture && imageRegionSelectionControl1.CurrentTexture != null)
                {
                    imageRegionSelectionControl1.CurrentTexture.Dispose();
                }

                imageRegionSelectionControl1.CurrentTexture = texture;
                InspectableTexture.Texture = texture;

            }
            SetSelectionTextureCoordinates(topTexture, bottomTexture, leftTexture, rightTexture);

        }

        private void SetSelectionTextureCoordinates(float topTexturePixel, float bottomTexturePixel, float leftTexturePixel, float rightTexturePixel)
        {
            Texture2D texture = imageRegionSelectionControl1.CurrentTexture;
            imageRegionSelectionControl1.DesiredSelectorCount = 1;
            imageRegionSelectionControl1.RectangleSelector.Left = leftTexturePixel;
            imageRegionSelectionControl1.RectangleSelector.Width = (rightTexturePixel - leftTexturePixel);

            imageRegionSelectionControl1.RectangleSelector.Top = topTexturePixel;
            imageRegionSelectionControl1.RectangleSelector.Height = (bottomTexturePixel - topTexturePixel);

            imageRegionSelectionControl1.RectangleSelector.Visible = true;
            imageRegionSelectionControl1.RectangleSelector.ShowHandles = true;
        }

        private void wireframeEditControls1_ZoomChanged(object sender, EventArgs e)
        {

            int zoomValue = wireframeEditControls1.PercentageValue;

            imageRegionSelectionControl1.ZoomValue = zoomValue;
        }

        void HandleMouseWheelZoom(object sender, EventArgs e)
        {
            wireframeEditControls1.PercentageValue = imageRegionSelectionControl1.ZoomValue;
        }
    }
}
