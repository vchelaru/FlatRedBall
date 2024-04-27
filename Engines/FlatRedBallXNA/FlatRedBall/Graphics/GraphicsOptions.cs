using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using FlatRedBall.IO;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

#if !MONOGAME && !FNA
    using System.Windows.Forms;
#endif

namespace FlatRedBall.Graphics
{

    public enum WindowedFullscreenMode
    {
        Windowed,
        Fullscreen,
        FullscreenBorderless
    }

    public class GraphicsOptions
    {
        #region Fields
        TextureFilter mTextureFilter = TextureFilter.Point;

        bool mSuspendDeviceReset = false;
        int mResolutionWidth;

        Game game;

        int mResolutionHeight;
        #endregion


        /// <summary>
        /// Event raised when the resolution or orientation changes.
        /// </summary>
        // Implementation note: Prior to 2016, FlatRedBall included
        // both SizeOrOrientationChanged and FlatRedBallServices.CornerGrabbingResize.
        // SizeOrOrientationChanged worked fine on all platforms (I think) except for PC.
        // SizeOrOrinetationChanged didn't get raised when clicking the maximize/minimize button.
        // To address this, custom code in FlatRedBallServices will be raising SizeOrOrientationChanged
        // on PC, and all other platforms continue to use the regular implementation.
        public event EventHandler SizeOrOrientationChanged;

        #region Fields

        #region XML Docs
        /// <summary>
        /// The texture loading color key
        /// </summary>
        #endregion
        public Color TextureLoadingColorKey = Color.Black;


        private WindowedFullscreenMode windowedFullscreenMode = WindowedFullscreenMode.Windowed;

#if !MONOGAME && !FNA
        // For some reason setting to fullscreen can crash things but setting the border style to none helps.
        System.Windows.Forms.FormBorderStyle mWindowedBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D; 
#endif

        bool mUseMultiSampling;
    
        #region XML Docs
        /// <summary>
        /// Set to true to suspend device reset while loading from file
        /// </summary>
        #endregion
        private static bool IsLoading = false;

        private bool mIsInAReset = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the current texture filter. If SuspendDeviceReset has not been called then 
        /// this setter will immediately apply the texture filter.
        /// </summary>
        public TextureFilter TextureFilter
        {
            get { return mTextureFilter; }
            set
            {
                if (mTextureFilter != value)
                {
                    mTextureFilter = value;

                    if (!mSuspendDeviceReset)
                    {
                        #region If DEBUG, check the caps of the graphics card
#if DEBUG
                        ThrowExceptionIfFilterIsntSupported(value);

#endif
                        #endregion

                        ForceRefreshSamplerState(0);
                    }
                }
            }
        }

        private static void ThrowExceptionIfFilterIsntSupported(TextureFilter value)
        {
            // For now do nothing, but we may want to perform some checks here against whether we're using REACH or HIDEF
        }

        /// <summary>
        /// Sets the width of the backbuffer and resets the device
        /// Use SetResolution() to set both width and height simultaneously
        /// </summary>
        public int ResolutionWidth
        {
            get { return mResolutionWidth; }
            set
            {
                mResolutionWidth = value;
                ResetDevice();
            }
        }

        /// <summary>
        /// Sets the height of the backbuffer and resets the device
        /// Use SetResolution() to set both width and height simultaneously
        /// </summary>
        public int ResolutionHeight
        {
            get { return mResolutionHeight; }
            set
            {
                mResolutionHeight = value;
                ResetDevice();
            }
        }

        /// <summary>
        /// Gets or sets the background color of all cameras
        /// </summary>
        [XmlIgnoreAttribute()]
        public Color BackgroundColor
        {
            get { return SpriteManager.Camera.BackgroundColor; }
            set
            {
                for (int i = 0; i < SpriteManager.Cameras.Count; i++)
                {
                    SpriteManager.Cameras[i].BackgroundColor = value;
                }
            }
        }


        public WindowedFullscreenMode WindowedFullscreenMode
        {
            get { return windowedFullscreenMode; }
            set
            {
                if(value != windowedFullscreenMode)
                {
                    int oldWidth = mResolutionWidth;
                    int oldHeight = mResolutionHeight;
                    windowedFullscreenMode = value;

                    if (!FlatRedBallServices.IsInitialized)
                    {
                        // It's possible for someone to instantiate a GraphicsOptions and
                        // set its FullScreen to true before FlatRedBall is created.  This is
                        // done so that the engine starts in full screen.  If this is the case,
                        // then we shouldn't do the remainder of the code in this property.
                        return;
                    }

#if !MONOGAME && !FNA
                    if (IsFullScreen)
                    {
                        mWindowedBorderStyle = ((Form)FlatRedBallServices.Owner).FormBorderStyle;
                        ((Form)FlatRedBallServices.Owner).FormBorderStyle = FormBorderStyle.None;
                    }
#endif

                    ResetDevice();
#if !MONOGAME && !FNA
                    if (!IsFullScreen)
                    {
                        ((Form)FlatRedBallServices.Owner).FormBorderStyle = mWindowedBorderStyle;
                    }
#endif
                    if (!IsFullScreen)
                    {
                        // When coming out of full screen the resolution is lost for some reason, so force it
                        SetResolution(oldWidth, oldHeight);
                    }
                }


            }
        }


        /// <summary>
        /// Sets the display mode to full screen
        /// Use SetFullScreen() to set the full-screen resolution and full-screen simultaneously
        /// </summary>
        public bool IsFullScreen
        {
            get { return windowedFullscreenMode == WindowedFullscreenMode.Fullscreen; }
            set
            {
                if (IsFullScreen != value)
                {
                    int oldWidth = mResolutionWidth;
                    int oldHeight = mResolutionHeight;

                    if(value)
                    {
                        windowedFullscreenMode = WindowedFullscreenMode.Fullscreen;
                    }
                    else
                    {
                        windowedFullscreenMode = WindowedFullscreenMode.Windowed;
                    }

					if (!FlatRedBallServices.IsInitialized)
					{
						// It's possible for someone to instantiate a GraphicsOptions and
						// set its FullScreen to true before FlatRedBall is created.  This is
						// done so that the engine starts in full screen.  If this is the case,
						// then we shouldn't do the remainder of the code in this property.
						return;
                    }

#if !MONOGAME && !FNA
                    if (IsFullScreen)
                    {
                        mWindowedBorderStyle = ((Form)FlatRedBallServices.Owner).FormBorderStyle;
                        ((Form)FlatRedBallServices.Owner).FormBorderStyle = FormBorderStyle.None;
                    }
#endif

                    ResetDevice();
#if !MONOGAME && !FNA
                    if (!IsFullScreen)
                    {
                        ((Form)FlatRedBallServices.Owner).FormBorderStyle = mWindowedBorderStyle;
                    }
#endif
                    if (!IsFullScreen)
                    {
                        // When coming out of full screen the resolution is lost for some reason, so force it
                        SetResolution(oldWidth, oldHeight);
                    }
                }
            }
        }

        /// <summary>
        /// Enables or disables multisampling
        /// </summary>
        public bool UseMultiSampling
        {
            get { return mUseMultiSampling; }
            set
            {
                mUseMultiSampling = value;
                ResetDevice();
            }
        }
        

        #endregion

        #region Constructor

        public GraphicsOptions()
            : this(null, null)
        {

        }


        public GraphicsOptions(Game game, GraphicsDeviceManager graphics)
        {

            // We used to call this here, but that makes HandleClientSizeOrOrientationChange get called first, before 
            // internal code, and that means that custom code gets logic before internal, which can result in resolutions being wrong...
            //if (game != null)
            //{
            //    game.Window.ClientSizeChanged += new EventHandler<EventArgs>(HandleClientSizeOrOrientationChange);
            //}
            this.game = game;

            if (graphics != null)
            {
                mResolutionWidth = graphics.PreferredBackBufferWidth;
                mResolutionHeight = graphics.PreferredBackBufferHeight;
            }

            mTextureFilter = Microsoft.Xna.Framework.Graphics.TextureFilter.Linear;
            
            mUseMultiSampling = false;

#if MONODROID
            if (graphics != null)
            {
                if(graphics.IsFullScreen)
                {
                    this.windowedFullscreenMode = WindowedFullscreenMode.Fullscreen;
                }
                else
                {
                    this.windowedFullscreenMode = WindowedFullscreenMode.Windowed;
                }
            }
#endif
            

#if !MONODROID
            #region Get Resolution

            SuspendDeviceReset();

            //graphicsOptions.ResolutionWidth = graphics.GraphicsDevice.DisplayMode.Width;// game.Window.ClientBounds.Width;
            //graphicsOptions.ResolutionHeight = graphics.GraphicsDevice.DisplayMode.Height;//game.Window.ClientBounds.Height;

            if (game != null)
            {

#if IOS || DESKTOP_GL
                // For UWP and WindowsGL projects the game.Window.ClientBounds is not accurate until after initialize, as explained here:
                // http://community.monogame.net/t/graphicsdevice-viewport-doesnt-return-the-real-size-of-uwp-game-window/7314/5
                ResolutionWidth = graphics.PreferredBackBufferWidth;
				ResolutionHeight = graphics.PreferredBackBufferHeight;

#else
                ResolutionWidth = game.Window.ClientBounds.Width;
                ResolutionHeight = game.Window.ClientBounds.Height;
#endif
            }


            if (graphics != null)
            {
            }

            ResumeDeviceReset();

            #endregion
#endif

        }

        // temp for testing:
        public void CallSizeOrOrientationChanged()
        {
            SizeOrOrientationChanged?.Invoke(this, null);
        }

        void HandleClientSizeOrOrientationChange(object sender, EventArgs e)
        {
#if !WINDOWS && !FNA
            SizeOrOrientationChanged?.Invoke(this, null);
#endif
        }

        #endregion

        #region Methods

        public void Initialize()
        {
            if (game != null)
            {
                game.Window.ClientSizeChanged += new EventHandler<EventArgs>(HandleClientSizeOrOrientationChange);
            }
        }

        static readonly SamplerState PointMirror = new SamplerState
        {
            AddressU = TextureAddressMode.Mirror,
            AddressV = TextureAddressMode.Mirror,
            AddressW = TextureAddressMode.Mirror,
            Filter = TextureFilter.Point
        };

        static readonly SamplerState LinearMirror = new SamplerState
        {
            AddressU = TextureAddressMode.Mirror,
            AddressV = TextureAddressMode.Mirror,
            AddressW = TextureAddressMode.Mirror,
            Filter = TextureFilter.Linear
        };

        static readonly SamplerState AnisotropicMirror = new SamplerState
        {
            AddressU = TextureAddressMode.Mirror,
            AddressV = TextureAddressMode.Mirror,
            AddressW = TextureAddressMode.Mirror,
            Filter = TextureFilter.Anisotropic
        };

        internal void ForceRefreshSamplerState() { ForceRefreshSamplerState(0); }
        internal void ForceRefreshSamplerState(int index)
        {
            switch (Renderer.TextureAddressMode)
            {
                case Microsoft.Xna.Framework.Graphics.TextureAddressMode.Clamp:
                    if (mTextureFilter == Microsoft.Xna.Framework.Graphics.TextureFilter.Point)
                    {
                        Renderer.GraphicsDevice.SamplerStates[index] = SamplerState.PointClamp;
                    }
                    else if (mTextureFilter == Microsoft.Xna.Framework.Graphics.TextureFilter.Linear)
                    {
                        Renderer.GraphicsDevice.SamplerStates[index] = SamplerState.LinearClamp;
                    }
                    else if(mTextureFilter == TextureFilter.Anisotropic)
                    {
                        Renderer.GraphicsDevice.SamplerStates[index] = SamplerState.AnisotropicClamp;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                    break;
                case Microsoft.Xna.Framework.Graphics.TextureAddressMode.Mirror:

                    if (mTextureFilter == Microsoft.Xna.Framework.Graphics.TextureFilter.Point)
                    {
                        Renderer.GraphicsDevice.SamplerStates[index] = PointMirror;
                    }
                    else if (mTextureFilter == Microsoft.Xna.Framework.Graphics.TextureFilter.Linear)
                    {
                        Renderer.GraphicsDevice.SamplerStates[index] = LinearMirror;
                    }
                    else if(mTextureFilter == TextureFilter.Anisotropic)
                    {
                        Renderer.GraphicsDevice.SamplerStates[index] = AnisotropicMirror;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                    break;
                case Microsoft.Xna.Framework.Graphics.TextureAddressMode.Wrap:
                    if (mTextureFilter == Microsoft.Xna.Framework.Graphics.TextureFilter.Point)
                    {
                        Renderer.GraphicsDevice.SamplerStates[index] = SamplerState.PointWrap;
                    }
                    else if (mTextureFilter == Microsoft.Xna.Framework.Graphics.TextureFilter.Linear)
                    {
                        Renderer.GraphicsDevice.SamplerStates[index] = SamplerState.LinearWrap;
                    }
                    else if(mTextureFilter == TextureFilter.Anisotropic)
                    {
                        Renderer.GraphicsDevice.SamplerStates[index] = SamplerState.AnisotropicWrap;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                    break;


            }

        }

        #region Setter Operations

        internal bool mHasResolutionBeenManuallySet = false;

        #region XML Docs
        /// <summary>
        /// Sets the resolution
        /// </summary>
        /// <param name="width">The new width</param>
        /// <param name="height">The new height</param>
        #endregion
        public void SetResolution(int width, int height)
        {
            mHasResolutionBeenManuallySet = true;

            mResolutionWidth = width;
            mResolutionHeight = height;
            ResetDevice();


            // Not sure why but the GameWindow's resolution change doesn't fire
            // That's okay, we now have a custom event for it.  Glue will generate against this:
#if !WINDOWS && !FNA

            SizeOrOrientationChanged?.Invoke(this, null);
#endif
        }   

        public void SetResolution(int width, int height, bool isFullscreen)
        {
            WindowedFullscreenMode windowedFullscreenMode;
            if (isFullscreen)
            {
                windowedFullscreenMode = WindowedFullscreenMode.Fullscreen;
            }
            else
            {
                windowedFullscreenMode = WindowedFullscreenMode.Windowed;
            }

            SetResolution(width, height, windowedFullscreenMode);
        }

        public void SetResolution(int width, int height, WindowedFullscreenMode windowedFullscreenMode)
        { 
            this.windowedFullscreenMode = windowedFullscreenMode;

            mResolutionWidth = width;
            mResolutionHeight = height;
            ResetDevice();

#if ANDROID
            var activity = FlatRedBallServices.Game.Services.GetService<Activity>();
            var activityWindow = activity?.Window;

            if (activityWindow != null)
            {
                if (windowedFullscreenMode == WindowedFullscreenMode.Fullscreen)
                {
                    activityWindow.DecorView.SystemUiFlags =
                        //activityWindow.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)
                        (Android.Views.SystemUiFlags.ImmersiveSticky | Android.Views.SystemUiFlags.HideNavigation |
                         Android.Views.SystemUiFlags.Fullscreen | Android.Views.SystemUiFlags.Immersive);
                }
                else
                {
                    activityWindow.DecorView.SystemUiFlags = Android.Views.SystemUiFlags.Visible;
                }
            }
#endif


#if !WINDOWS && !FNA

            SizeOrOrientationChanged?.Invoke(this, null);
#endif
        }

        /// <summary>
        /// Sets the display mode to full-screen and sets the resolution
        /// </summary>
        /// <param name="width">The new width</param>
        /// <param name="height">The new height</param>
        public void SetFullScreen(int width, int height)
        {
            SetResolution(width, height, WindowedFullscreenMode.Fullscreen);
        }




        //#region XML Docs
        ///// <summary>
        ///// Sets multisampling on with the specified options
        ///// </summary>
        ///// <param name="type">The type of multisampling</param>
        ///// <param name="quality">The quality of multisampling</param>
        //#endregion
        //public void SetMultiSampling(MultiSampleType type, int quality)
        //{
        //    mUseMultiSampling = (type != MultiSampleType.None);

        //    mMultiSampleType = type;
        //    mMultiSampleQuality = 0; // What a terrible option.  See:
        //    //http://windows-tech.info/5/eff2355699026ce0.php
        //    ResetDevice();
        //}

#endregion

            #region Reset Operations

            #region XML Docs
        /// <summary>
        /// Suspends the device reset when options are changed
        /// </summary>
            #endregion
        public void SuspendDeviceReset()
        {
            mSuspendDeviceReset = true;
        }

            #region XML Docs
        /// <summary>
        /// Resumes the device reset when options are changed
        /// </summary>
            #endregion
        public void ResumeDeviceReset()
        {
            mSuspendDeviceReset = false;
        }

        /// <summary>
        /// Resets the device if the device is not currently rendering, if the graphics device is not null, and if 
        /// device resetting is not suspended.
        /// </summary>
        public void ResetDevice()
        {
            if (mIsInAReset)
            {
                return;
            }

            #region If the Renderer.Graphics is null that means the engine is not loaded yet
            if (!mSuspendDeviceReset && Renderer.Graphics == null)
            {
                throw new InvalidOperationException("Can't reset the device right now because the Renderer's Graphics are null. " +
                    "Are you attempting to change the GraphicsOption's properties prior to the creation of FlatRedBallServices? " +
                    "If so you must call SuspendDeviceReset before changing properties, then ResumeDeviceReset after the properties " +
                    "are set, but before calling FlatRedBallServices.InitializeFlatRedBall.  Otherwise, move the property-changing " +
                    "code to after FlatRedBall is initialized.");

            }
            #endregion

            #region Else, check to make sure device resetting is not suspended and the GraphicsOptions are not loading
            else if (!mSuspendDeviceReset && !GraphicsOptions.IsLoading)
            {
                mIsInAReset = true;

                // Reset the graphics device manager
                if (FlatRedBallServices.mGraphics != null)
                {
                    if(windowedFullscreenMode == WindowedFullscreenMode.Fullscreen)
                    {
                        ThrowExceptionIfFullScreenResolutionNotSupported(mResolutionWidth, mResolutionHeight);
                    }


                    // Set window size
                    FlatRedBallServices.mGraphics.PreferredBackBufferWidth = mResolutionWidth;
                    FlatRedBallServices.mGraphics.PreferredBackBufferHeight = mResolutionHeight;
                    FlatRedBallServices.mGraphics.PreferMultiSampling = mUseMultiSampling;
                    FlatRedBallServices.mGraphics.IsFullScreen = windowedFullscreenMode == WindowedFullscreenMode.Fullscreen;

#if DESKTOP_GL
                    // for borderless the user must set the GraphicsDeviceManager.HardwareModeSwitch to false
                    http://community.monogame.net/t/how-to-implement-borderless-fullscreen-on-desktopgl-project/8359
                    FlatRedBallServices.mGraphics.IsFullScreen = 
                        windowedFullscreenMode == WindowedFullscreenMode.Fullscreen || 
                        windowedFullscreenMode == WindowedFullscreenMode.FullscreenBorderless


                        ;

#else
                    FlatRedBallServices.mGraphics.IsFullScreen = windowedFullscreenMode == WindowedFullscreenMode.Fullscreen;

#endif



                    try
                    {
                        FlatRedBallServices.mGraphics.ApplyChanges();

                    }
                    // No longer needed since we are always going to use a sample quality of 0
                    //catch (Exception e)
                    //{
                    //    int qualityLevels = 0;
                    //    bool allowed = GraphicsAdapter.DefaultAdapter.CheckDeviceMultiSampleType(DeviceType.Hardware,
                    //        SurfaceFormat.Color, false, mMultiSampleType, out qualityLevels);

                    //    throw e;
                    //}


                    finally
                    {
                        mIsInAReset = false;
                    }
                }
                // Prepare the presentation parameters
                PresentationParameters presParams = FlatRedBallServices.GraphicsDevice.PresentationParameters;

                // Sets the presentation parameters
                SetPresentationParameters(ref presParams);

                // Reset the device
                if (FlatRedBallServices.mGraphics != null)
                {
    #if !MONODROID
                    while (FlatRedBallServices.mGraphics.GraphicsDevice.GraphicsDeviceStatus == GraphicsDeviceStatus.Lost ||
                        FlatRedBallServices.mGraphics.GraphicsDevice.GraphicsDeviceStatus == GraphicsDeviceStatus.NotReset)
                    {
                        int m = 3;
                        m += 32;
                        m /= 32;
                    }
    #endif
                }

    #if MONOGAME
                // Resetting crashes monogame currently, but we can still react as if a reset happened
                FlatRedBallServices.graphics_DeviceReset(null, null);
    #else
                FlatRedBallServices.GraphicsDevice.Reset(presParams);
    #endif

                // When the device resets the render states could get screwed up.  Force the 
                // blend state changes in case they were changed but nothing later changes them
                // back.
                // Hm, this seems to cause a crash because the mCurrentEffect isn't set yet
                //Renderer.ForceSetColorOperation(Renderer.ColorOperation);
                Renderer.ForceSetBlendOperation();
                mIsInAReset = false;
            }
            #endregion
        }

        private static void ThrowExceptionIfFullScreenResolutionNotSupported(int proposedWidth, int proposedHeight)
        {
            // Victor Chelaru
            // February 26, 2014
            // Android doesn't have 
            // the ability to run on 
            // multiple resolutions, so 
            // we won't do any checks here.
            // Should this also be !IOS?
#if DEBUG && !ANDROID && !UWP
            bool foundResolution = false;

            var supportedDisplay = GraphicsAdapter.DefaultAdapter.SupportedDisplayModes;

            foreach (DisplayMode mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
            {
                if (mode.Width == proposedWidth && mode.Height == proposedHeight)
                {
                    foundResolution = true;
                }
            }
            if (!foundResolution)
            {
                string message = $"The resolution {proposedWidth} x {proposedHeight} is not supported in full screen mode.  Supported resolutions:\n";
                message += "(width x height)\n";

                foreach (var value in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
                {
                    message += value.Width + "x" + value.Height + "\n";
                }


                throw new InvalidOperationException(message);
            }
#endif
        }

        #region XML Docs
        /// <summary>
        /// Sets the presentation parameters
        /// </summary>
        /// <param name="presentationParameters">The structure to set parameters in</param>
        #endregion
        public void SetPresentationParameters(ref PresentationParameters presentationParameters)
        {
            presentationParameters.BackBufferWidth = mResolutionWidth;
            presentationParameters.BackBufferHeight = mResolutionHeight;
            presentationParameters.IsFullScreen = windowedFullscreenMode == WindowedFullscreenMode.Fullscreen ;
        }

        #endregion

            #region File Operations

            #region XML Docs
        /// <summary>
        /// Save the graphics options to a file
        /// </summary>
        /// <param name="fileName">The file name of the graphics options file</param>
            #endregion
        public void Save(string fileName)
        {
            FileManager.XmlSerialize<GraphicsOptions>(this, fileName);
        }

            #region XML Docs
        /// <summary>
        /// Load the graphics options from file
        /// </summary>
        /// <param name="fileName">The file name of the graphics options file</param>
            #endregion
        public static GraphicsOptions FromFile(string fileName)
        {
            GraphicsOptions options;
            try
            {
                GraphicsOptions.IsLoading = true;
                options = FileManager.XmlDeserialize<GraphicsOptions>(fileName);
            }
            catch
            {
                options = new GraphicsOptions(); // failed to open the file, oh well, make a new one.
            }
            finally
            {
                GraphicsOptions.IsLoading = false;
            }

            return options;
        }

            #endregion

        #endregion
    }

}
