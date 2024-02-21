// This is a generated file created by Glue. To change this file, edit the camera settings in Glue.
// To access the camera settings, push the camera icon.
using Camera = FlatRedBall.Camera;
namespace GlueTestProject
{
    public class CameraSetupData
    {
        public bool IsGenerateCameraDisplayCodeEnabled { get; set; }
        public float Scale { get; set; }
        public float ScaleGum { get; set; }
        public bool Is2D { get; set; }
        public int ResolutionWidth { get; set; }
        public int ResolutionHeight { get; set; }
        public decimal? AspectRatio { get; set; }
        public decimal? AspectRatio2 { get; set; }
        public bool AllowWindowResizing { get; set; }
        public bool IsFullScreen { get; set; }
        public ResizeBehavior ResizeBehavior { get; set; }
        public ResizeBehavior ResizeBehaviorGum { get; set; }
        public WidthOrHeight DominantInternalCoordinates { get; set; }
        public Microsoft.Xna.Framework.Graphics.TextureFilter TextureFilter { get; set; }
        
        public decimal? EffectiveAspectRatio
        {
            get
            {
                if(AspectRatio2 == null)
                {
                    return AspectRatio;
                }
                else if(AspectRatio == null)
                {
                    return AspectRatio2;
                }
                else if(FlatRedBall.FlatRedBallServices.ClientHeight == 0)
                {
                    // just in case:
                    return AspectRatio;
                }
                else
                {
                    // Neither AspectRatio nor 2 are null here

                    var resolutionAspectRatio = FlatRedBall.FlatRedBallServices.ClientWidth / (decimal)FlatRedBall.FlatRedBallServices.ClientHeight;

                    var minAspect = System.Math.Min(AspectRatio.Value, AspectRatio2.Value);
                    var maxAspect = System.Math.Max(AspectRatio.Value, AspectRatio2.Value);

                    if(resolutionAspectRatio < minAspect)
                    {
                        return minAspect;
                    }
                    else if(resolutionAspectRatio > maxAspect)
                    {
                        return maxAspect;
                    }
                    else
                    {
                        // it's begween min and max, so return the resolution aspect ratio
                        return resolutionAspectRatio;
                    }
                }
            }
        }

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
        public static Microsoft.Xna.Framework.GraphicsDeviceManager GraphicsDeviceManager { get; private set; }
        public static CameraSetupData Data = new CameraSetupData
        {
            Scale = 100f,
            IsGenerateCameraDisplayCodeEnabled = true,
            ResolutionWidth = 1000,
            ResolutionHeight = 600,
            Is2D = true,
            IsFullScreen = false,
            AllowWindowResizing = true,
            TextureFilter = Microsoft.Xna.Framework.Graphics.TextureFilter.Linear,
            ResizeBehavior = ResizeBehavior.StretchVisibleArea,
            ScaleGum = 100f,
            ResizeBehaviorGum = ResizeBehavior.StretchVisibleArea,
            DominantInternalCoordinates = WidthOrHeight.Height,
        }
        ;
        /// <summary>
        /// Applies resolution and aspect ratio values to the FlatRedBall camera. If Gum is part of the project,
        /// then the Gum resolution will be applied. Note that this does not call Layout on the contained Gum objects,
        /// so this may need to be called explicitly if ResetCamera is called in custom code.
        /// </summary>
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
            else
            {
                cameraToReset.UsePixelCoordinates3D(0);
                var zoom = cameraToReset.DestinationRectangle.Height / (float)Data.ResolutionHeight;
                cameraToReset.Z /= zoom; 
            }
            SetAspectRatioTo(Data.EffectiveAspectRatio, Data.DominantInternalCoordinates, Data.ResolutionWidth, Data.ResolutionHeight);
            ResetGumResolutionValues();
        }
        internal static void SetupCamera (Camera cameraToSetUp, Microsoft.Xna.Framework.GraphicsDeviceManager graphicsDeviceManager) 
        {
            CameraSetup.GraphicsDeviceManager = graphicsDeviceManager;
            FlatRedBall.FlatRedBallServices.GraphicsOptions.TextureFilter = Data.TextureFilter;
            ResetWindow();
            ResetCamera(cameraToSetUp);
            ResetGumResolutionValues();
            FlatRedBall.FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged += HandleResolutionChange;
        }
        internal static void ResetWindow () 
        {
            #if WINDOWS || DESKTOP_GL
            FlatRedBall.FlatRedBallServices.Game.Window.AllowUserResizing = Data.AllowWindowResizing;
            if (Data.IsFullScreen)
            {
                #if DESKTOP_GL
                #if DEBUG
                if (GraphicsDeviceManager == null)
                {
                    throw new System.InvalidOperationException("ResetWindow cannot be called until SetupCamera is called first");
                }
                #endif
                GraphicsDeviceManager.HardwareModeSwitch = false;
                FlatRedBall.FlatRedBallServices.Game.Window.Position = new Microsoft.Xna.Framework.Point(0,0);
                FlatRedBall.FlatRedBallServices.GraphicsOptions.SetResolution(Microsoft.Xna.Framework.Graphics.GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width, Microsoft.Xna.Framework.Graphics.GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height, FlatRedBall.Graphics.WindowedFullscreenMode.FullscreenBorderless);
                #elif FNA
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
                var width = (int)(Data.ResolutionWidth * Data.Scale / 100.0f);
                var height = (int)(Data.ResolutionHeight * Data.Scale / 100.0f);
                // subtract to leave room for windows borders
                var maxWidth = Microsoft.Xna.Framework.Graphics.GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width - 6;
                var maxHeight = Microsoft.Xna.Framework.Graphics.GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - 28;
                width = System.Math.Min(width, maxWidth);
                height = System.Math.Min(height, maxHeight);
                #if MONOGAME
                if (FlatRedBall.FlatRedBallServices.Game.Window.Position.Y < 25)
                {
                    FlatRedBall.FlatRedBallServices.Game.Window.Position = new Microsoft.Xna.Framework.Point(FlatRedBall.FlatRedBallServices.Game.Window.Position.X, 25);
                }
                #endif
                FlatRedBall.FlatRedBallServices.GraphicsOptions.SetResolution(width, height);
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
                FlatRedBall.FlatRedBallServices.GraphicsOptions.SetResolution(Data.ResolutionWidth, Data.ResolutionHeight);
                var newWindowSize = new Windows.Foundation.Size(Data.ResolutionWidth, Data.ResolutionHeight);
                Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TryResizeView(newWindowSize); 
            }
            #endif
        }
        private static void HandleResolutionChange (object sender, System.EventArgs args) 
        {
            SetAspectRatioTo(Data.EffectiveAspectRatio, Data.DominantInternalCoordinates, Data.ResolutionWidth, Data.ResolutionHeight);
            if (Data.Is2D && Data.ResizeBehavior == ResizeBehavior.IncreaseVisibleArea)
            {
                FlatRedBall.Camera.Main.OrthogonalHeight = FlatRedBall.Camera.Main.DestinationRectangle.Height / (Data.Scale/ 100.0f);
                FlatRedBall.Camera.Main.FixAspectRatioYConstant();
            }
            ResetGumResolutionValues();
        }
        /// <summary>
        /// Sets the Gum GraphicalUiElement's CanvasWidth and CanvasHeight as well as all Layer Zoom values.
        /// </summary>
        public static void ResetGumResolutionValues () 
        {
            if (Data.ResizeBehaviorGum == ResizeBehavior.IncreaseVisibleArea)
            {
                global::RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom = Data.Scale/100.0f;
                Gum.Wireframe.GraphicalUiElement.CanvasWidth = FlatRedBall.Camera.Main.DestinationRectangle.Width;
                Gum.Wireframe.GraphicalUiElement.CanvasHeight = FlatRedBall.Camera.Main.DestinationRectangle.Height; 
            }
            else
            {
                Gum.Wireframe.GraphicalUiElement.CanvasHeight = Data.ResolutionHeight / (Data.ScaleGum/100.0f);
                if (Data.EffectiveAspectRatio != null)
                {
                    

                    if(Data.DominantInternalCoordinates == WidthOrHeight.Height)
                    {
                        Gum.Wireframe.GraphicalUiElement.CanvasHeight = Data.ResolutionHeight / (Data.ScaleGum / 100.0f);
                        Gum.Wireframe.GraphicalUiElement.CanvasWidth = FlatRedBall.Math.MathFunctions.RoundToInt(Gum.Wireframe.GraphicalUiElement.CanvasHeight * (double)Data.EffectiveAspectRatio.Value);
                    }
                    else
                    {
                        Gum.Wireframe.GraphicalUiElement.CanvasWidth = Data.ResolutionWidth / (Data.ScaleGum/100.0f);
                        Gum.Wireframe.GraphicalUiElement.CanvasHeight = FlatRedBall.Math.MathFunctions.RoundToInt(Gum.Wireframe.GraphicalUiElement.CanvasHeight / (double)Data.EffectiveAspectRatio.Value);
                    }                    

                    var resolutionAspectRatio = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth / (decimal)FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight;
                    int destinationRectangleWidth;
                    int destinationRectangleHeight;
                    int x = 0;
                    int y = 0;
                    if (Data.EffectiveAspectRatio.Value > resolutionAspectRatio)
                    {
                        destinationRectangleWidth = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth;
                        destinationRectangleHeight = FlatRedBall.Math.MathFunctions.RoundToInt(destinationRectangleWidth / (float)Data.EffectiveAspectRatio.Value);
                    }
                    else
                    {
                        destinationRectangleHeight = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight;
                        destinationRectangleWidth = FlatRedBall.Math.MathFunctions.RoundToInt(destinationRectangleHeight * (float)Data.EffectiveAspectRatio.Value);
                    }

                    var canvasHeight = Gum.Wireframe.GraphicalUiElement.CanvasHeight;
                    var zoom = (float)destinationRectangleHeight / (float)Gum.Wireframe.GraphicalUiElement.CanvasHeight;
                    if(global::RenderingLibrary.SystemManagers.Default != null)
                    {
                        global::RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom = zoom;

                        foreach(var layer in global::RenderingLibrary.SystemManagers.Default.Renderer.Layers)
                        {
                            if(layer.LayerCameraSettings != null)
                            {
                                layer.LayerCameraSettings.Zoom = zoom;
                            }
                        }
                    }
                    

                }
                else
                {
                    

                    // since a fixed aspect ratio isn't specified, adjust the width according to the 
                    // current game aspect ratio and the canvas height
                    var currentAspectRatio = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth / (float)
                        FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight;
                    Gum.Wireframe.GraphicalUiElement.CanvasWidth =
                        Gum.Wireframe.GraphicalUiElement.CanvasHeight * currentAspectRatio;

                    var graphicsHeight = Gum.Wireframe.GraphicalUiElement.CanvasHeight;
                    var windowHeight = FlatRedBall.Camera.Main.DestinationRectangle.Height;
                    var zoom = windowHeight / (float)graphicsHeight;
                    if(global::RenderingLibrary.SystemManagers.Default != null)
                    {
                        global::RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom = zoom;
                        foreach(var layer in global::RenderingLibrary.SystemManagers.Default.Renderer.Layers)
                        {
                            if(layer.LayerCameraSettings != null)
                            {
                                layer.LayerCameraSettings.Zoom = zoom;
                            }
                        }
                    }
                    
                }
            }
        }
        private static void SetAspectRatioTo (decimal? aspectRatio, WidthOrHeight dominantInternalCoordinates, int desiredWidth, int desiredHeight) 
        {
            var resolutionAspectRatio = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth / (decimal)FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight;
            int destinationRectangleWidth;
            int destinationRectangleHeight;
            int x = 0;
            int y = 0;
            if (aspectRatio == null)
            {
                destinationRectangleWidth = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth;
                destinationRectangleHeight = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight;
            }
            else if (aspectRatio > resolutionAspectRatio)
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
            for (int i = 0; i < FlatRedBall.SpriteManager.Cameras.Count; i++)
            {
                var camera = FlatRedBall.SpriteManager.Cameras[i];
                int currentX = x;
                int currentY = y;
                int currentWidth = destinationRectangleWidth;
                int currentHeight = destinationRectangleHeight;
                switch(camera.CurrentSplitScreenViewport)
                {
                    case  Camera.SplitScreenViewport.TopLeft:
                        currentWidth /= 2;
                        currentHeight /= 2;
                        break;
                    case  Camera.SplitScreenViewport.TopRight:
                        currentX = x + destinationRectangleWidth / 2;
                        currentWidth /= 2;
                        currentHeight /= 2;
                        break;
                    case  Camera.SplitScreenViewport.BottomLeft:
                        currentY = y + destinationRectangleHeight / 2;
                        currentWidth /= 2;
                        currentHeight /= 2;
                        break;
                    case  Camera.SplitScreenViewport.BottomRight:
                        currentX = x + destinationRectangleWidth / 2;
                        currentY = y + destinationRectangleHeight / 2;
                        currentWidth /= 2;
                        currentHeight /= 2;
                        break;
                    case  Camera.SplitScreenViewport.TopHalf:
                        currentHeight /= 2;
                        break;
                    case  Camera.SplitScreenViewport.BottomHalf:
                        currentY = y + destinationRectangleHeight / 2;
                        currentHeight /= 2;
                        break;
                    case  Camera.SplitScreenViewport.LeftHalf:
                        currentWidth /= 2;
                        break;
                    case  Camera.SplitScreenViewport.RightHalf:
                        currentX = x + destinationRectangleWidth / 2;
                        currentWidth /= 2;
                        break;
                }
                camera.DestinationRectangle = new Microsoft.Xna.Framework.Rectangle(currentX, currentY, currentWidth, currentHeight);
                if (dominantInternalCoordinates == WidthOrHeight.Height)
                {
                    camera.OrthogonalHeight = desiredHeight;
                    camera.FixAspectRatioYConstant();
                }
                else
                {
                    camera.OrthogonalWidth = desiredWidth;
                    camera.FixAspectRatioXConstant();
                }
            }
        }
        
#if WINDOWS
        internal static readonly System.IntPtr HWND_TOPMOST = new System.IntPtr(-1);
        internal static readonly System.IntPtr HWND_NOTOPMOST = new System.IntPtr(-2);
        internal static readonly System.IntPtr HWND_TOP = new System.IntPtr(0);
        internal static readonly System.IntPtr HWND_BOTTOM = new System.IntPtr(1);
    
        [System.Flags]
        internal enum SetWindowPosFlags : uint
        {
            IgnoreMove = 0x0002,
            IgnoreResize = 0x0001,
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct RECT

        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GetWindowRect(System.IntPtr hWnd, out RECT lpRect);

    
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetWindowPos(System.IntPtr hWnd, System.IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        public static Microsoft.Xna.Framework.Rectangle GetWindowRectangle()
        {
            var hWnd = FlatRedBall.FlatRedBallServices.Game.Window.Handle;

            GetWindowRect(hWnd, out RECT rectInner);

            return new Microsoft.Xna.Framework.Rectangle(
                rectInner.Left,
                rectInner.Top,
                rectInner.Right - rectInner.Left,
                rectInner.Bottom - rectInner.Top);

        }

        public static void SetWindowPosition(int x, int y)
        {
            var hWnd = FlatRedBall.FlatRedBallServices.Game.Window.Handle;
            SetWindowPos(
                hWnd,
                HWND_TOPMOST,
                x, y,
                0, 0, //FlatRedBallServices.GraphicsOptions.ResolutionWidth, FlatRedBallServices.GraphicsOptions.ResolutionHeight,
                SetWindowPosFlags.IgnoreResize
            );
        }

        public static void SetWindowAlwaysOnTop()
        {
            var hWnd = FlatRedBall.FlatRedBallServices.Game.Window.Handle;
            SetWindowPos(
                hWnd,
                HWND_TOPMOST,
                0, 0,
                0, 0, //FlatRedBallServices.GraphicsOptions.ResolutionWidth, FlatRedBallServices.GraphicsOptions.ResolutionHeight,
                SetWindowPosFlags.IgnoreMove | SetWindowPosFlags.IgnoreResize
            );
        }

        public static void UnsetWindowAlwaysOnTop()
        {
            var hWnd = FlatRedBall.FlatRedBallServices.Game.Window.Handle;

            SetWindowPos(
                hWnd,
                HWND_NOTOPMOST,
                0, 0,
                0, 0, //FlatRedBallServices.GraphicsOptions.ResolutionWidth, FlatRedBallServices.GraphicsOptions.ResolutionHeight,
                SetWindowPosFlags.IgnoreMove | SetWindowPosFlags.IgnoreResize
            );
        }
#else
        public static void SetWindowAlwaysOnTop()
            {
                // not supported on this platform, do nothing
            }

            public static void UnsetWindowAlwaysOnTop()
            {
                // not supported on this platform, do nothings
            }

#endif

    }
}
