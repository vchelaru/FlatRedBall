    // This is a generated file created by Glue. To change this file, edit the camera settings in Glue.
    // To access the camera settings, push the camera icon.
    using Camera = FlatRedBall.Camera;
    namespace FlatRedBallWpf
    {
        public class CameraSetupData
        {
            public float Scale { get; set; }
            public bool Is2D { get; set; }
            public int ResolutionWidth { get; set; }
            public int ResolutionHeight { get; set; }
            public decimal? AspectRatio { get; set; }
            public bool AllowWidowResizing { get; set; }
            public bool IsFullScreen { get; set; }
            public ResizeBehavior ResizeBehavior { get; set; }
            public WidthOrHeight DominantInternalCoordinates { get; set; }
        }
        public enum ResizeBehavior
        {
            StretchVisibleArea,
            IncreaseVisibleArea
        }
        public enum WidthOrHeight
        {
            Width,
            Height
        }
        internal static class CameraSetup
        {
            static Microsoft.Xna.Framework.GraphicsDeviceManager graphicsDeviceManager;
            public static CameraSetupData Data = new CameraSetupData
            {
                Scale = 100f,
                ResolutionWidth = 800,
                ResolutionHeight = 600,
                Is2D = true,
                IsFullScreen = false,
                AllowWidowResizing = false,
                ResizeBehavior = ResizeBehavior.StretchVisibleArea,
                DominantInternalCoordinates = WidthOrHeight.Height,
            }
            ;
            internal static void ResetCamera (Camera cameraToReset = null) 
            {
                if (cameraToReset == null)
                {
                    cameraToReset = FlatRedBall.Camera.Main;
                }
                cameraToReset.Orthogonal = Data.Is2D;
                if (Data.Is2D)
                {
                    cameraToReset.OrthogonalHeight = Data.ResolutionHeight;
                    cameraToReset.OrthogonalWidth = Data.ResolutionWidth;
                    cameraToReset.FixAspectRatioYConstant();
                }
                if (Data.AspectRatio != null)
                {
                    SetAspectRatioTo(Data.AspectRatio.Value, Data.DominantInternalCoordinates, Data.ResolutionWidth, Data.ResolutionHeight);
                }
            }
            internal static void SetupCamera (Camera cameraToSetUp, Microsoft.Xna.Framework.GraphicsDeviceManager graphicsDeviceManager) 
            {
                CameraSetup.graphicsDeviceManager = graphicsDeviceManager;
                ResetWindow();
                ResetCamera(cameraToSetUp);
                FlatRedBall.FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged += HandleResolutionChange;
            }
            internal static void ResetWindow () 
            {
                #if WINDOWS || DESKTOP_GL
                FlatRedBall.FlatRedBallServices.Game.Window.AllowUserResizing = Data.AllowWidowResizing;
                if (Data.IsFullScreen)
                {
                    #if DESKTOP_GL
                    graphicsDeviceManager.HardwareModeSwitch = false;
                    FlatRedBall.FlatRedBallServices.GraphicsOptions.SetResolution(Microsoft.Xna.Framework.Graphics.GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width, Microsoft.Xna.Framework.Graphics.GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height, FlatRedBall.Graphics.WindowedFullscreenMode.FullscreenBorderless);
                    #elif WINDOWS
                    System.IntPtr hWnd = FlatRedBall.FlatRedBallServices.Game.Window.Handle;
                    var control = System.Windows.Forms.Control.FromHandle(hWnd);
                    var form = control.FindForm();
                    form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                    form.WindowState = System.Windows.Forms.FormWindowState.Maximized;
                    #endif
                }
                else
                {
                    FlatRedBall.FlatRedBallServices.GraphicsOptions.SetResolution((int)(Data.ResolutionWidth * Data.Scale/ 100.0f), (int)(Data.ResolutionHeight * Data.Scale/ 100.0f));
                }
                #elif IOS || ANDROID
                FlatRedBall.FlatRedBallServices.GraphicsOptions.SetFullScreen(FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth, FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight);
                #elif UWP
                if (Data.IsFullScreen)
                {
                    FlatRedBall.FlatRedBallServices.GraphicsOptions.SetFullScreen(Data.ResolutionWidth, Data.ResolutionHeight);
                }
                else
                {
                    FlatRedBall.FlatRedBallServices.GraphicsOptions.SetResolution((int)(Data.ResolutionWidth * Data.Scale/ 100.0f), (int)(Data.ResolutionHeight * Data.Scale/ 100.0f));
                    var newWindowSize = new Windows.Foundation.Size((int)(Data.ResolutionWidth * Data.Scale/ 100.0f), (int)(Data.ResolutionHeight * Data.Scale/ 100.0f));
                    Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TryResizeView(newWindowSize); 
                }
                #endif
            }
            private static void HandleResolutionChange (object sender, System.EventArgs args) 
            {
                if (Data.AspectRatio != null)
                {
                    SetAspectRatioTo(Data.AspectRatio.Value, Data.DominantInternalCoordinates, Data.ResolutionWidth, Data.ResolutionHeight);
                }
                if (Data.Is2D && Data.ResizeBehavior == ResizeBehavior.IncreaseVisibleArea)
                {
                    FlatRedBall.Camera.Main.OrthogonalHeight = FlatRedBall.Camera.Main.DestinationRectangle.Height / (Data.Scale/ 100.0f);
                    FlatRedBall.Camera.Main.FixAspectRatioYConstant();
                }
            }
            private static void SetAspectRatioTo (decimal aspectRatio, WidthOrHeight dominantInternalCoordinates, int desiredWidth, int desiredHeight) 
            {
                var resolutionAspectRatio = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth / (decimal)FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight;
                int destinationRectangleWidth;
                int destinationRectangleHeight;
                int x = 0;
                int y = 0;
                if (aspectRatio > resolutionAspectRatio)
                {
                    destinationRectangleWidth = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth;
                    destinationRectangleHeight = FlatRedBall.Math.MathFunctions.RoundToInt(destinationRectangleWidth / (float)aspectRatio);
                    y = (FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight - destinationRectangleHeight) / 2;
                }
                else
                {
                    destinationRectangleHeight = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight;
                    destinationRectangleWidth = FlatRedBall.Math.MathFunctions.RoundToInt(destinationRectangleHeight * (float)aspectRatio);
                    x = (FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth - destinationRectangleWidth) / 2;
                }
                FlatRedBall.Camera.Main.DestinationRectangle = new Microsoft.Xna.Framework.Rectangle(x, y, destinationRectangleWidth, destinationRectangleHeight);
                if (dominantInternalCoordinates == WidthOrHeight.Height)
                {
                    FlatRedBall.Camera.Main.OrthogonalHeight = desiredHeight;
                    FlatRedBall.Camera.Main.FixAspectRatioYConstant();
                }
                else
                {
                    FlatRedBall.Camera.Main.OrthogonalWidth = desiredWidth;
                    FlatRedBall.Camera.Main.FixAspectRatioXConstant();
                }
            }
        }
    }
