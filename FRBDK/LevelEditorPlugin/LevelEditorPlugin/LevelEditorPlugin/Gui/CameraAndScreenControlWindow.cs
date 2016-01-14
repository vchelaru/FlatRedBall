using System;
using FlatRedBall.Gui;
using EditorObjects.Gui;
using FlatRedBall;

namespace LevelEditor.Gui
{
    public sealed class CameraAndScreenControlWindow : Window
    {
        #region Fields

        readonly ComboBox _comboBox;
        readonly ComboBox _flickeringComboBox;
        readonly Button _cameraPropertiesButton;
        readonly Button _toOriginButton;

        #endregion

        #region Event Methods

        static void ToOriginClick(Window callingWindow)
        {
            SpriteManager.Camera.X = 0;
            SpriteManager.Camera.Y = 0;
            SpriteManager.Camera.Z = 40;
        }

        void ComboBoxItemClick(Window callingWindow)
        {
            SpriteManager.Camera.Position = new Microsoft.Xna.Framework.Vector3(0, 0, 40);

            var configuration =
                (CameraPropertyGrid.CameraConfiguration)_comboBox.SelectedObject;

            switch (configuration)
            {
                case CameraPropertyGrid.CameraConfiguration.Android2D_320X480:
                    FlatRedBallServices.GraphicsOptions.SetResolution(
                        480, 320);

                    SpriteManager.Camera.UsePixelCoordinates();
                    break;
                case CameraPropertyGrid.CameraConfiguration.Android2D800:
                    FlatRedBallServices.GraphicsOptions.SetResolution(
                        800, 480);

                    SpriteManager.Camera.UsePixelCoordinates();
                    break;
                case CameraPropertyGrid.CameraConfiguration.Android2D854:
                    FlatRedBallServices.GraphicsOptions.SetResolution(
                        854, 480);

                    SpriteManager.Camera.UsePixelCoordinates();
                    break;
                case CameraPropertyGrid.CameraConfiguration.Default3D:
                    FlatRedBallServices.GraphicsOptions.SetResolution(
                        800, 600);
                    SpriteManager.Camera.Z = 40;
                    SpriteManager.Camera.FieldOfView = (float)Math.PI / 4.0f;

                    SpriteManager.Camera.Orthogonal = false;
                    break;
                case CameraPropertyGrid.CameraConfiguration.Silverlight:
                case CameraPropertyGrid.CameraConfiguration.Default2D:
                    FlatRedBallServices.GraphicsOptions.SetResolution(
                        800, 600);
                    SpriteManager.Camera.UsePixelCoordinates();

                    break;
                case CameraPropertyGrid.CameraConfiguration.WindowsPhone3D:
                    FlatRedBallServices.GraphicsOptions.SetResolution(
                        800, 480);

                    SpriteManager.Camera.Z = 40;
                    SpriteManager.Camera.FieldOfView = (float)Math.PI / 4.0f;

                    SpriteManager.Camera.Orthogonal = false;

                    break;
                case CameraPropertyGrid.CameraConfiguration.WindowsPhoneWVGA2DTall:
                    FlatRedBallServices.GraphicsOptions.SetResolution(
                        480, 800);

                    SpriteManager.Camera.UsePixelCoordinates();
                    break;
                case CameraPropertyGrid.CameraConfiguration.WindowsPhoneWVGA2DWide:

                    FlatRedBallServices.GraphicsOptions.SetResolution(
                        800, 480);
                    SpriteManager.Camera.UsePixelCoordinates();

                    break;
                case CameraPropertyGrid.CameraConfiguration.Standard240:
                    FlatRedBallServices.GraphicsOptions.SetResolution(
                        320, 240);
                    SpriteManager.Camera.UsePixelCoordinates();
                    break;
                case CameraPropertyGrid.CameraConfiguration.Standard300:
                    FlatRedBallServices.GraphicsOptions.SetResolution(
                        1024, 768);
                    SpriteManager.Camera.UsePixelCoordinates(false, 400, 300);
                    break;
            }
        }

        static void ShowCameraProperties(Window callingWindow)
        {
            GuiManager.ObjectDisplayManager.GetObjectDisplayerForObject(SpriteManager.Camera);
        }

        #endregion

        public CameraAndScreenControlWindow(Cursor cursor)
            : base(cursor)
        {

            ScaleX = 16.5f;
            ScaleY = 4.3f;
            X = ScaleX;
            Y = ScaleY + MoveBarHeight;
            HasMoveBar = true;

            #region Create the CameraConfiguration ComboBox

            _comboBox = new ComboBox(cursor);
            
            AddWindow(_comboBox);
            _comboBox.Y = 2;
            _comboBox.ScaleX = 16;
            Array values = Enum.GetValues(typeof(CameraPropertyGrid.CameraConfiguration));

            _comboBox.ItemClick += ComboBoxItemClick;


            foreach (object value in values)
            {
                _comboBox.AddItem(value.ToString(), value);
            }
            #endregion

            #region Create the Flickering ComboBox

            _flickeringComboBox = new ComboBox(cursor);

            AddWindow(_flickeringComboBox);
            _flickeringComboBox.Y = 4.5f;
            _flickeringComboBox.ScaleX = 16;
            _flickeringComboBox.ItemClick += FlickeringItemClick;

            _flickeringComboBox.AddItem("Flickering On", true);
            _flickeringComboBox.AddItem("Flickering Off", false);

            _flickeringComboBox.SelectItem(0);

            #endregion

            #region Create the Show Camera Property Grid button

            _cameraPropertiesButton = new Button(cursor);
            AddWindow(_cameraPropertiesButton);
            _cameraPropertiesButton.ScaleX = 8.5f;
            _cameraPropertiesButton.Text = "Camera Properties";
            _cameraPropertiesButton.X = _cameraPropertiesButton.ScaleX + .5f;
            _cameraPropertiesButton.Y = 7f;
            _cameraPropertiesButton.Click += ShowCameraProperties;
            #endregion

            #region Create the To Origin button

            _toOriginButton = new Button(cursor);
            AddWindow(_toOriginButton);
            _toOriginButton.ScaleX = 5f;
            _toOriginButton.Text = "To Origin";
            _toOriginButton.X = _toOriginButton.ScaleX + 2*_cameraPropertiesButton.ScaleX + 1;
            _toOriginButton.Y = 7f;
            _toOriginButton.Click += ToOriginClick;

            #endregion

            PropertyGrid.SetPropertyGridTypeAssociation(typeof(Camera), typeof(CameraPropertyGridReduced));

        }

        void FlickeringItemClick(Window callingWindow)
        {
            var flickeringOn = (bool)_flickeringComboBox.SelectedObject;

            EditorLogic.FlickeringOn = flickeringOn;
        }



    }
}
