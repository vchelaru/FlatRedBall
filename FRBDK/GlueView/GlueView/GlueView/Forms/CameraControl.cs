using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Math;
using FlatRedBall.Glue.GuiDisplay;
using GlueView.Facades;
using FlatRedBall.Glue.SaveClasses;
using GlueView.Plugin;
using GlueView.Forms.PropertyGrids;
using EditorObjects;

namespace GlueView.Forms
{

    public partial class CameraControl : UserControl
    {
        #region Fields

        const string GlueCameraSettings = "Glue Camera Settings";

        static CameraControl mSelf;

        #endregion

        public static CameraControl Self
        {
            get
            {
                return mSelf;
            }
        }

        public CameraControl()
        {
            mSelf = this;
            InitializeComponent();

            PopulateCameraConfigComboBox();
        }

        private void PopulateCameraConfigComboBox()
        {
            // We're going to allow the user to use the camera settings from Glue as well
            this.CameraConfigurationComboBox.Items.Add(GlueCameraSettings);

            Array values = Enum.GetValues(typeof(CameraConfiguration));

            foreach (object value in values)
            {
                this.CameraConfigurationComboBox.Items.Add(value);
            }

            this.CameraConfigurationComboBox.Text = GlueCameraSettings;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshCamera();
        }

        public void RefreshCamera()
        {
            SpriteManager.Camera.Position = new Microsoft.Xna.Framework.Vector3(0, 0, 40);
            object selectedItem = CameraConfigurationComboBox.SelectedItem;

            float yResolution = 600;
            if (selectedItem is CameraConfiguration)
            {
                ApplySettingsFromCameraConfigurationEnum(ref yResolution);
            }
            else if (selectedItem is string)
            {
                if ((string)selectedItem == GlueCameraSettings)
                {
                    GlueProjectSave glueProject = GlueViewState.Self.CurrentGlueProject;

                    if (glueProject != null)
                    {
                        if(glueProject.DisplaySettings != null)
                        {
                            yResolution = SetCameraFromDisplaySettings(yResolution, glueProject.DisplaySettings);
                        }
                        else
                        {
                            yResolution = SetCameraFromOldValues(yResolution, glueProject);
                        }
                    }
                }
            }



            GuiManager.OverridingFieldOfView = MathFunctions.GetAspectRatioForSameSizeAtResolution(yResolution);
            this.propertyGrid1.Refresh();

            PluginManager.ReactToResolutionChange();
        }

        private float SetCameraFromDisplaySettings(float yResolution, DisplaySettings displaySettings)
        {
            SpriteManager.Camera.Orthogonal = displaySettings.Is2D;
            if(displaySettings.GenerateDisplayCode)
            {
                SetResolution(displaySettings.ResolutionWidth, displaySettings.ResolutionHeight);
                yResolution = displaySettings.ResolutionHeight;
            }

            SpriteManager.Camera.OrthogonalWidth = displaySettings.ResolutionWidth;
            SpriteManager.Camera.OrthogonalHeight = displaySettings.ResolutionHeight;
            return yResolution;
        }

        private float SetCameraFromOldValues(float yResolution, GlueProjectSave glueProject)
        {
            SpriteManager.Camera.Orthogonal = glueProject.In2D;

            if (glueProject.SetResolution)
            {
                SetResolution(glueProject.ResolutionWidth, glueProject.ResolutionHeight);
                yResolution = glueProject.ResolutionHeight;

            }
            if (glueProject.SetOrthogonalResolution)
            {
                SpriteManager.Camera.OrthogonalWidth = glueProject.OrthogonalWidth;
                SpriteManager.Camera.OrthogonalHeight = glueProject.OrthogonalHeight;
            }
            else
            {
                // If we don't set it explicitly then default to the resolution:
                SpriteManager.Camera.OrthogonalWidth = glueProject.ResolutionWidth;
                SpriteManager.Camera.OrthogonalHeight = glueProject.ResolutionHeight;
            }

            return yResolution;
        }

        private void ApplySettingsFromCameraConfigurationEnum(ref float yResolution)
        {
            CameraConfiguration configuration =
                (CameraConfiguration)CameraConfigurationComboBox.SelectedItem;

            //(CameraPropertyGrid.CameraConfiguration)comboBox.SelectedObject;

            switch (configuration)
            {
                case CameraConfiguration.Android2D_320X480:
                    yResolution = 320;

                    SetResolution(
                        480, 320);

                    SpriteManager.Camera.UsePixelCoordinates();
                    break;
                case CameraConfiguration.Android2D800:
                    yResolution = 480;
                    SetResolution(
                        800, 480);

                    SpriteManager.Camera.UsePixelCoordinates();
                    break;
                case CameraConfiguration.Android2D854:
                    yResolution = 480;
                    SetResolution(
                        854, 480);

                    SpriteManager.Camera.UsePixelCoordinates();
                    break;
                case CameraConfiguration.Default3D:
                    yResolution = 600;
                    SetResolution(
                        800, 600);
                    SpriteManager.Camera.Z = 40;
                    SpriteManager.Camera.FieldOfView = (float)System.Math.PI / 4.0f;

                    SpriteManager.Camera.Orthogonal = false;
                    break;
                case CameraConfiguration.Silverlight:
                case CameraConfiguration.Default2D:
                    yResolution = 600;
                    SetResolution(
                        800, 600);
                    SpriteManager.Camera.UsePixelCoordinates(false);

                    break;
                case CameraConfiguration.WindowsPhone3D:
                    yResolution = 480;
                    SetResolution(
                        800, 480);

                    SpriteManager.Camera.Z = 40;
                    SpriteManager.Camera.FieldOfView = (float)System.Math.PI / 4.0f;

                    SpriteManager.Camera.Orthogonal = false;

                    break;
                case CameraConfiguration.WindowsPhoneWVGA2DTall:
                    yResolution = 800;
                    SetResolution(
                        480, 800);

                    SpriteManager.Camera.UsePixelCoordinates();
                    break;
                case CameraConfiguration.WindowsPhoneWVGA2DWide:
                    yResolution = 480;
                    SetResolution(
                        800, 480);
                    SpriteManager.Camera.UsePixelCoordinates();

                    break;
                case CameraConfiguration.Standard240:
                    yResolution = 240;
                    SetResolution(
                        320, 240);
                    SpriteManager.Camera.UsePixelCoordinates();
                    break;
                case CameraConfiguration.Standard300:
                    yResolution = 300;
                    SetResolution(
                        400, 300);
                    SpriteManager.Camera.UsePixelCoordinates(false, 400, 300);
                    break;
            }
        }

        void SetResolution(int width, int height)
        {
            if (width != FlatRedBallServices.GraphicsOptions.ResolutionWidth ||
                height != FlatRedBallServices.GraphicsOptions.ResolutionHeight)
            {
                FlatRedBallServices.GraphicsOptions.SetResolution(width, height);
            }
        }

        private void FlickeringCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            EditorLogic.FlickeringOn = FlickeringCheckBox.Checked;
        }

        private void ToOriginButton_Click(object sender, EventArgs e)
        {
            SpriteManager.Camera.X = 0;
            SpriteManager.Camera.Y = 0;
            SpriteManager.Camera.Z = 40;

            this.propertyGrid1.Refresh();
        }

        private void CameraControl_Load(object sender, EventArgs e)
        {
            if(!DesignMode)
            {
                CameraDisplayer displayer = new CameraDisplayer();
                displayer.Instance = SpriteManager.Camera;

                displayer.PropertyGrid = this.propertyGrid1;

            }
        }

    }
}
