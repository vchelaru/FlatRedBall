using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Input;
#if !WINDOWS_PHONE && !MONODROID
using FlatRedBall.Content.Gui;
#endif
using FlatRedBall.Utilities;
using FlatRedBall.Graphics;
using Microsoft.Xna.Framework;
using FlatRedBall.Gui.PropertyGrids;

#if !FRB_MDX
using Microsoft.Xna.Framework.Graphics;
#else
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Drawing;
#endif

namespace FlatRedBall.Gui
{
    internal struct GuiTextureSwitch
    {
        public bool SwitchBackAfterPrimitive;
        public Texture2D Texture;
        public int Index;
    }

    public delegate void GuiMessage(Window callingWindow);
    public static partial class GuiManager
    {
        // Every frame the GuiManager counts vertices.
        // This variable is used in a few different methods
        // So it's declared at class scope.
        static int mVerticesCounted;

#if FRB_MDX
        // Used in the FileWindow
		static internal Texture2D mUpDirectory;

        // This is the "tool tip" that appears
        // when the mouse hovers over a button.
		static Texture2D mCursorTextBox;
#endif

        static Window mCursorTextBackground;
		static int mNumberVerticesToDraw;// = 0;
		internal static int VertexBufferNumber;

        internal static List<VertexBuffer> vertexBufferList;
        public static VertexBuffer vertexBuffer;

        static List<GuiTextureSwitch> mTextureSwitches;

		private static int[] renderOrder = new int[6];

        public static Texture2D guiTexture;
#if FRB_MDX
		static GraphicsStream verts;
#else
        static SpriteBatch mSpriteBatch;
        static VertexDeclaration mVertexDeclaration;

#endif

        #region members to speed up the Draw method

        static int drawToOnThisVB;
        static int drawnOnThisVB;
        static int totalDrawn;

        static int textureChange;

        static int VBOn;// = 0;

        static int numPasses;// = 0;

        static int numTrianglesToDraw = 2000 * GuiManager.VertexBufferNumber + GuiManager.mNumberVerticesToDraw / 3;


        #endregion

        static internal CollapseItem mCollapseItemDraggedOff;

        public static CollapseItem CollapseItemDraggedOff
        {
            get { return mCollapseItemDraggedOff; }
        }
        
        public static int NumberVerticesToDraw
        {
            get { return mNumberVerticesToDraw; }
        }

        static void RenderingBasedInitializize()
        {

#if FRB_MDX
#elif FRB_XNA
                mSpriteBatch = new SpriteBatch(FlatRedBallServices.GraphicsDevice);

#if XNA4
                mVertexDeclaration = VertexPositionColorTexture.VertexDeclaration;
#else
                mVertexDeclaration  = new VertexDeclaration(
                Renderer.GraphicsDevice, VertexPositionColorTexture.VertexElements);
#endif
#endif




            // This is not added to the GuiManager because we want it manually drawn
            // in the Draw method if the cursor is over a window that is going to show text
            mCursorTextBackground = new Window(Cursor);
            mCursorTextBackground.ScaleX = 1;
            mCursorTextBackground.ScaleY = 1;
            mCursorTextBackground.Visible = true;


            mTextureSwitches = new List<GuiTextureSwitch>();

#if !SILVERLIGHT
            vertexBufferList = new List<VertexBuffer>();

            CreateVertexBuffers();

            renderOrder[0] = 3;
            renderOrder[1] = 0;
            renderOrder[2] = 1;
            renderOrder[3] = 3;
            renderOrder[4] = 1;
            renderOrder[5] = 2;
#endif


        }

		static public Button AddButton()
		{
            Button button = new Button(Cursor);
            mWindowArray.Add(button);
			return button;
		}

		static public Window AddWindow()
		{
            Window window = new Window(Cursor);
            mWindowArray.Add(window);
			window.Parent = null;
            window.ScaleX = window.ScaleY = 2;
			return window;
		}

        static public Window AddWindow(string textureToUse)
        {
            Window window = new Window(
                textureToUse, Cursor, InternalGuiContentManagerName);

            mWindowArray.Add(window);
            window.Parent = null;
            return window;
        }

		public static FileWindow AddFileWindow()
		{
            
            FileWindow fileWindow = new FileWindow(Cursor);
            fileWindow.Parent = null;

            InputManager.ReceivingInput = fileWindow.mListBox;

            mDominantWindows.Add(fileWindow);

			return fileWindow;
		}

        static public ListBox AddListBox()
        {
            ListBox listBox = new ListBox(Cursor);
            mWindowArray.Add(listBox);
            listBox.Parent = null;
            return listBox;
        }


        public static MenuStrip AddMenuStrip()
        {
            MenuStrip menuStrip = new MenuStrip(Cursor);
            menuStrip.Parent = null;

            mWindowArray.Add(menuStrip);
            return menuStrip;
        }

        static public MultiButtonMessageBox AddMultiButtonMessageBox()
        {
            MultiButtonMessageBox mbmb = new MultiButtonMessageBox(Cursor);
            mbmb.Parent = null;

            mDominantWindows.Add(mbmb);

            return mbmb;
        }
        public static OkCancelWindow AddOkCancelWindow()
        {
            OkCancelWindow okCancelWindow = new OkCancelWindow(Cursor);
            mWindowArray.Add(okCancelWindow);
            okCancelWindow.Parent = null;

            InputManager.ReceivingInput = okCancelWindow;

            return okCancelWindow;
        }
        static public TimeLine AddTimeLine()
        {
            TimeLine timeLine = new TimeLine(Cursor);
            mWindowArray.Add(timeLine);
            timeLine.Parent = null;
            return timeLine;
        }

        #region XML Docs
        /// <summary>
        /// Adds a Window to the GuiManager which will disappear the next time
        /// the user clicks.
        /// </summary>
        /// <param name="windowToAdd">The Window to add.</param>
        #endregion
        public static void AddPerishableWindow(Window windowToAdd)
        {
            mPerishableArray.Add(windowToAdd);

        }


        public static ListBox AddPerishableListBox()
        {
            ListBox listBox = new ListBox(Cursor);

            listBox.Parent = null;

            listBox.ScaleX = 10;
            listBox.ScaleY = 10;

            PositionTopLeftToCursor(listBox);


            AddPerishableWindow(listBox);

            return listBox;
        }

        public static PropertyGrid<T> AddPropertyGrid<T>()
        {
            PropertyGrid<T> propertyGrid = new PropertyGrid<T>(Cursor);
            AddWindow(propertyGrid);
            return propertyGrid;


        }

        public static void Animate()
        {
            for (int i = 0; i < mWindowArray.Count; i++)
            {
                Window asWindow = mWindowArray[i] as Window;

                if (asWindow != null && asWindow.Visible)
                {
                    asWindow.AnimateSelf();
                }
            }
        }

#if !WINDOWS_PHONE && !MONODROID
        public static WindowSaveCollection GetCurrentLayout()
        {
            // First, make sure that each window is uniquely named
            for (int i = 0; i < mWindowArray.Count; i++)
            {
                IWindow window = mWindowArray[i];

                if (!StringFunctions.IsNameUnique(window, mWindowArray))
                {
                    throw new InvalidOperationException("The Window of type " + window.GetType() + " has a non-unique name");
                }
            }

            WindowSaveCollection wsc = WindowSaveCollection.FromRuntime(mWindowArray);

            return wsc;
        }
#endif

        static internal int GetNumberOfVerticesToDraw()
        {
            int i = 0;
            foreach (IWindow w in mWindowArray)
            {
                if (w.Visible && w.GuiManagerDrawn)
                {
                    Window asWindow = w as Window;

                    i += asWindow.GetNumberOfVerticesToDraw();
                    i += asWindow.GetNumberOfVerticesToDrawFloating();
                }
            }

            foreach (IWindow w in mDominantWindows)
            {
                if (w.Visible && w.GuiManagerDrawn)
                {
                    Window asWindow = w as Window;

                    i += asWindow.GetNumberOfVerticesToDraw();
                    i += asWindow.GetNumberOfVerticesToDrawFloating();
                }
            }

            foreach (IWindow w in mPerishableArray)
            {
                if (w.Visible && w.GuiManagerDrawn)
                {
                    Window asWindow = w as Window;

                    i += asWindow.GetNumberOfVerticesToDraw();
                    i += asWindow.GetNumberOfVerticesToDrawFloating();
                }

            }

            return i;

        }

        static public Window GetWindowOver(float cameraRelativeX, float cameraRelativeY)
        {
            Window windowOver = null;

            foreach (Window w in mPerishableArray)
            {
                windowOver = w.GetWindowOver(cameraRelativeX, cameraRelativeY);
                if (windowOver != null)
                    return windowOver;
            }

            foreach (Window w in mDominantWindows)
            {
                windowOver = w.GetWindowOver(cameraRelativeX, cameraRelativeY);
                if (windowOver != null)
                    return windowOver;
            }

            foreach (Window w in mWindowArray)
            {
                windowOver = w.GetWindowOver(cameraRelativeX, cameraRelativeY);
                if (windowOver != null)
                    return windowOver;
            }

            return null;

        }


        public static void KeepAllWindowsInScreen()
        {
            foreach (Window w in mWindowArray)
            {
                w.KeepInScreen();
            }
        }


        public static void PersistPerishableThroughNextClick(Window perishableWindow)
        {
            if (!mPerishableArray.Contains(perishableWindow))
            {
                throw new ArgumentException("The argument Window is not a perishable Window");
            }
            else
            {
                mPerishableWindowsToSurviveClick.Add(perishableWindow);
            }

        }


        public static void PositionTopLeftToCursor(Window window)
        {
            window.X = UnmodifiedXEdge + Cursor.XForUI + window.ScaleX;
            window.Y = (UnmodifiedYEdge - Cursor.YForUI) + window.ScaleY;
        }

        public static void RefreshTextSize()
        {
            float pixelsPerUnit =
                SpriteManager.Camera.DestinationRectangle.Width /
                (2 * GuiManager.XEdge);

            // Text Height should not be divided by two since it is not scale
            TextHeight =
                TextManager.DefaultFont.LineHeightInPixels /
                    pixelsPerUnit;


            TextSpacing = TextHeight / 2.0f;

            foreach (Window w in mWindowArray)
            {
                if (w is PropertyGrid)
                {
                    ((PropertyGrid)w).UpdateScaleAndWindowPositions();
                }
            }
        }


#if !WINDOWS_PHONE && !MONODROID
        public static void SetLayout(WindowSaveCollection windowSaveCollection)
        {
            SetLayout(windowSaveCollection, VisibilityPreservation.PreserveVisibility);
        }

        public static void SetLayout(WindowSaveCollection windowSaveCollection, VisibilityPreservation visibilityPreservation)
        {
            Dictionary<IWindow, Vector2> scaleDictionary = null;

            WindowSave.ApplyVisible = false;
            WindowSave.ApplyMinimumScales = false;

            bool preservePropertyGridScales = true;

            if (preservePropertyGridScales)
            {
                scaleDictionary = new Dictionary<IWindow, Vector2>();

                for (int i = 0; i < mWindowArray.Count; i++)
                {
                    if (mWindowArray[i] is PropertyGrid)
                    {
                        scaleDictionary.Add(mWindowArray[i], new Vector2(mWindowArray[i].ScaleX, mWindowArray[i].ScaleY));
                    }
                }
            }

            windowSaveCollection.ApplyTo(mWindowArray, FlatRedBallServices.GlobalContentManager);

            for (int i = 0; i < mWindowArray.Count; i++)
            {
                if (mWindowArray[i] is Window)
                {
                    ((Window)mWindowArray[i]).KeepInScreen();
                }
            }


            if (scaleDictionary != null)
            {
                foreach (KeyValuePair<IWindow, Vector2> kvp in scaleDictionary)
                {
                    kvp.Key.ScaleX = kvp.Value.X;
                    kvp.Key.ScaleY = kvp.Value.Y;
                }
            }
        }
#endif

        public static void ShiftBy(float x, float y)
        {
            ShiftBy(x, y, true);
        }


        public static void ShiftBy(float x, float y, bool shiftSpriteFrameGui)
        {


            foreach (Window w in mWindowArray)
            {
                if (w.GuiManagerDrawn || shiftSpriteFrameGui)
                {
                    w.X += x;
                    w.Y += y;
                }
            }

            foreach (Window w in mDominantWindows)
            {
                if (w.GuiManagerDrawn || shiftSpriteFrameGui)
                {
                    w.X += x;
                    w.Y += y;
                }
            }

        }


        public static MessageBox ShowMessageBox(string message, string name)
        {
            MessageBox messageBox = new MessageBox(Cursor);
            messageBox.Activate(message, name);
            InputManager.ReceivingInput = messageBox;

            mDominantWindows.Add(messageBox);

            return messageBox;
        }


        public static OkCancelWindow ShowOkCancelWindow(string message, string name)
        {
            OkCancelWindow okCancelWindow = new OkCancelWindow(Cursor);
            okCancelWindow.Message = message;
            okCancelWindow.Name = name;

            //			Activate
            //			((OkCancelWindow)dominantWindow).Update();
            okCancelWindow.Activate(message, name);
            //			InputManager.ReceivingInput = dominantWindow;

            mDominantWindows.Add(okCancelWindow);

            return okCancelWindow;

        }


        public static TextInputWindow ShowTextInputWindow(string textToShow, string name)
        {
            TextInputWindow tiw = new TextInputWindow(Cursor);
            tiw.Activate(textToShow, name);
            InputManager.ReceivingInput = tiw.mTextBox;

            mDominantWindows.Add(tiw);

            return tiw;
        }

        public static void TileWindow(IWindow windowToTile)
        {
            TileWindow(mWindowArray, windowToTile);
        }


        public static void TileWindows()
        {
            Window.KeepWindowsInScreen = false;

            List<IWindow> tiledWindows = new List<IWindow>();
            List<IWindow> untiledWindows = new List<IWindow>();

            // do a reverse loop here so that the reverse loop later is actually foward
            // in terms of membership order.

            for (int i = mWindowArray.Count - 1; i > -1; i--)
            {
                IWindow window = mWindowArray[i];

                if (window.Visible)
                {
                    if (window is MenuStrip || window is InfoBar)
                    {
                        tiledWindows.Add(window);
                    }
                    else
                    {
                        untiledWindows.Add(window);
                        window.X = window.ScaleX;
                        window.Y = window.ScaleY;
                    }
                }
            }

            // Now we have all of our untiled windows.  Loop through them, position them, then 
            // move them to the tiledWindows list.

            for (int i = untiledWindows.Count - 1; i > -1; i--)
            {
                IWindow untiledWindow = untiledWindows[i];

                TileWindow(tiledWindows, untiledWindow);


                untiledWindows.RemoveAt(i);
                tiledWindows.Add(untiledWindow);

            }

            Window.KeepWindowsInScreen = true;
        }

        private static void TileWindow(List<IWindow> tiledWindows, IWindow untiledWindow)
        {
            bool isTiled = false;
            while (!isTiled)
            {
                if (untiledWindow is Window && ((Window)untiledWindow).HasMoveBar)
                {
                    untiledWindow.Y += Window.MoveBarHeight;
                }
                Window windowThatUntiledOverlaps = null;

                foreach (Window tiledWindow in tiledWindows)
                {

                    if (untiledWindow != tiledWindow && // this first check is put here so that this method can be called by the GuiManager on just a random Window and pass its own WindowArray as the tiled windows
                        untiledWindow.OverlapsWindow(tiledWindow))
                    {
                        windowThatUntiledOverlaps = tiledWindow;
                        break;
                    }
                }

                if (windowThatUntiledOverlaps != null)
                {
                    // Don't set isTiled to true - do it again
                    untiledWindow.Y = .001f + // just to avoid floating point issues
                        windowThatUntiledOverlaps.Y + windowThatUntiledOverlaps.ScaleY + untiledWindow.ScaleY;
                }
                else
                {
                    isTiled = true;
                }
            }
        }

        static public void WriteVerts(VertexPositionColorTexture[] mVertices)
        {
#if XNA4
            throw new NotImplementedException();
#else
#if FRB_MDX
            verts.Write(mVertices);
#else
            FlatRedBallServices.GraphicsDevice.Vertices[0].SetSource(null, 0, 0);
            vertexBuffer.SetData<VertexPositionColorTexture>(mNumberVerticesToDraw * VertexPositionColorTexture.SizeInBytes,
                mVertices, 0, mVertices.Length, VertexPositionColorTexture.SizeInBytes);
    
#endif
            // We must make sure the vertices aren't on the device before changing data

            
            mNumberVerticesToDraw += mVertices.Length;

            CheckVBIncrement();
#endif
        }

        #region XML Docs
        /// <summary>
        /// Inserts a texture switch in the rendering of the GUI.  The argument textureToSwitch will be 
        /// used to draw just the next quad passed as 6 vertices.
        /// </summary>
        /// <remarks>
        /// This method is called from the Draw method of an object that is a Window or inherits from
        /// Window.  This notifies the GuiManager that the next 6 vertices passed through the
        /// GuiManager.verts.Write(v); call will be using a different texture.  After those 6
        /// are drawn, the GuiManager continues to draw the rest of the quads using the regular
        /// GUI texture.
        /// 
        /// The important thing to remember is that this method does NOT need to be called in pairs.
        ///
        /// </remarks>
        /// <param name="textureToSwitch">The texture to use for the following quad.</param>
        #endregion
        static internal void AddTextureSwitch(Texture2D textureToSwitch)
        {
            AddTextureSwitch(textureToSwitch, true);
        }

        static internal void AddTextureSwitch(Texture2D textureToSwitch, bool switchBack)
        {
            GuiTextureSwitch textureSwitch = new GuiTextureSwitch();
            textureSwitch.Texture = textureToSwitch;
            textureSwitch.SwitchBackAfterPrimitive = switchBack;
            textureSwitch.Index = 2000 * VertexBufferNumber + mNumberVerticesToDraw / 3;

            mTextureSwitches.Add(textureSwitch);
        }

        private static void UpDownReactToCursorPush()
        {
            if (mLastWindowWithFocus is UpDown &&
                // All windows can receive focus, but we only want to say that the text
                // box has changed if the text box was the one with focus and the user clicked
                // off of it.
                InputManager.ReceivingInput == ((UpDown)mLastWindowWithFocus).textBox &&
                Cursor.WindowOver != ((UpDown)mLastWindowWithFocus).textBox)
            {
                ((UpDown)mLastWindowWithFocus).textBoxChangeValue(mLastWindowWithFocus);
            }
        }

        private static void UpDownReactToPrimaryClick(Cursor c, ref FlatRedBall.Gui.IInputReceiver objectClickedOn)
        {
            if (c.WindowOver as FlatRedBall.Gui.UpDown != null &&
                c.IsOn(((UpDown)c.WindowOver).mTextBox as IWindow))
            {
                objectClickedOn = ((UpDown)c.WindowOver).mTextBox;
            }

        }

        private static void ButtonReactToPush()
        {

            Button buttonPushed = Cursor.WindowPushed as Button;
            if (buttonPushed != null && Cursor.WindowPushed as ToggleButton == null)
                buttonPushed.ButtonPushedState = ButtonPushedState.Up;
            else if (buttonPushed != null) // toggleButton
            {
                if (((ToggleButton)buttonPushed).IsPressed == false && ((ToggleButton)buttonPushed).ButtonPushedState == ButtonPushedState.Down)
                {
                    ((ToggleButton)buttonPushed).ButtonPushedState = ButtonPushedState.Up;
                }
                else if (((ToggleButton)buttonPushed).IsPressed == true && ((ToggleButton)buttonPushed).ButtonPushedState == ButtonPushedState.Up)
                {
                    ((ToggleButton)buttonPushed).ButtonPushedState = ButtonPushedState.Down;
                }


            }

        }

        static internal void CreateVertexBuffers()
        {
            VertexBuffer vertexBuffer;

            int vertexBufferCount = 13;

            for (int i = 0; i < vertexBufferCount; i++)
            {
#if FRB_MDX
                vertexBuffer = new VertexBuffer(typeof(VertexPositionColorTexture), 6000, Renderer.GraphicsDevice,
                    Usage.Dynamic | Usage.WriteOnly, VertexPositionColorTexture.Format, Pool.Default);
#elif MONOGAME
                vertexBuffer = new VertexBuffer(Renderer.GraphicsDevice,
                    typeof(VertexPositionColorTexture),
                    6000,
                    BufferUsage.None);
#elif XNA4

                vertexBuffer = new VertexBuffer(Renderer.GraphicsDevice,
                    VertexPositionColorTexture.VertexDeclaration,
                    6000,
                    BufferUsage.None);
#else
                vertexBuffer = new VertexBuffer(
                    Renderer.GraphicsDevice,
                    6000 * VertexPositionColorTexture.SizeInBytes,
                    BufferUsage.None);
                    
#endif
                if (i >= vertexBufferList.Count)
                {
                    vertexBufferList.Add(vertexBuffer);
                }
                else
                {
                    vertexBufferList[i] = vertexBuffer;
                }
            }
        }

        static internal void DisposeVertexBuffers()
        {
            for (int i = 0; i < vertexBufferList.Count; i++)
            {
                vertexBufferList[i].Dispose();
            }

            vertexBufferList.Clear();
        }




        static internal void Draw()
        {
            #region Return if UI's not enabled.
            if (mUIEnabled == false && !DrawCursorEvenIfThereIsNoUI)
                return;
            #endregion

            #region Return if there's nothing to draw

            if (mWindowArray.Count == 0 && mPerishableArray.Count == 0 && mDominantWindows.Count == 0 && !DrawCursorEvenIfThereIsNoUI)
            {
                return;
            }

            #endregion

            #region Initialize states (including camera matrices) and log values

            #region Set the GraphicsDevice states and matrices

            float oldFar = Camera.FarClipPlane;
            float oldNew = Camera.NearClipPlane;

            Camera.NearClipPlane = 99;
            Camera.FarClipPlane = 101;

            Vector3 oldCameraUp = Camera.UpVector;
            Camera.UpVector = new Vector3(0, 1, 0);

#if FRB_MDX
            if (SpriteManager.Exiting || SpriteManager.lostDevice) return;

            float oldFieldOfView = Camera.FieldOfView;
            
            Renderer.GraphicsDevice.RenderState.Lighting = false;

            Renderer.GraphicsDevice.Transform.Projection = Matrix.OrthoLH(XEdge * 2, YEdge * 2, 85, 110);

            Renderer.GraphicsDevice.Transform.View = Matrix.LookAtLH(
                new Vector3(-GetXOffsetForModifiedAspectRatio(), GetYOffsetForModifiedAspectRatio(), Camera.Z), new Vector3(-GetXOffsetForModifiedAspectRatio(), GetYOffsetForModifiedAspectRatio(), Camera.Z + 1), new Vector3(0, 1, 0));
			Renderer.GraphicsDevice.RenderState.ZBufferEnable = false;
			Renderer.GraphicsDevice.RenderState.ZBufferWriteEnable = false;

#else
            // This will use the default camera (index 0)'s viewport

#if !WINDOWS_PHONE && !MONODROID
            FlatRedBallServices.GraphicsDevice.Viewport = SpriteManager.Camera.GetViewport();
#endif
            Matrix oldRotationMatrix = Camera.RotationMatrix;
            Vector3 oldPosition = Camera.Position;
            bool oldOrthogonal = Camera.Orthogonal;
            float oldFieldOfView = Camera.FieldOfView;

            // For now the new position uses the camera's Z position.  This should be 
            // fixed and then the UI doesn't have to worry about setting its position 100 
            // units in front of the camera.  But for now we just want to get it to work.
            Camera.RotationMatrix = Matrix.Identity;


            Camera.Position = new Vector3(-GetXOffsetForModifiedAspectRatio(), GetYOffsetForModifiedAspectRatio(), Camera.Z);
            Camera.Orthogonal = false;

            if (!float.IsNaN(mOverridingFieldOfView))
            {
                Camera.FieldOfView = mOverridingFieldOfView;

            }
            Camera.UpdateViewProjectionMatrix();

            Camera.SetDeviceViewAndProjection(Renderer.Effect, false);

            Camera.Position = oldPosition;
            Camera.RotationMatrix = oldRotationMatrix;
            Camera.Orthogonal = oldOrthogonal;
            Camera.FieldOfView = oldFieldOfView;

            Renderer.SetCurrentEffect(Renderer.Effect, Camera);



#if XNA4
            if (Camera.ClearsDepthBuffer)
            {
                Renderer.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            }
#else

            Renderer.GraphicsDevice.RenderState.DepthBufferEnable = false;
            Renderer.GraphicsDevice.RenderState.DepthBufferWriteEnable = false;

            Renderer.GraphicsDevice.VertexDeclaration = mVertexDeclaration;
#endif


#endif


            #endregion

            renderingNotes.Clear();

            TextManager.mScaleForVertexBuffer = TextHeight / 2.0f;
            TextManager.mSpacingForVertexBuffer = TextSpacing;


            mNumberVerticesToDraw = 0;
            VertexBufferNumber = 0;

            mTextureSwitches.Clear();

            #endregion

            #region Get the mVerticesCounted to know how much to draw



            mVerticesCounted = GetNumberOfVerticesToDraw();

            #region Add the tool tip verts

            string stringToWrite = "";

            if (string.IsNullOrEmpty(mToolTipText) == false)
            {
                stringToWrite = mToolTipText;
            }

            if (ShowingCursorTextBox == true && string.IsNullOrEmpty(stringToWrite) == false)
            {
                mVerticesCounted += stringToWrite.Length * 6;

                mVerticesCounted += mCursorTextBackground.GetNumberOfVerticesToDraw();
            }

            #endregion

            #endregion

            if (mVerticesCounted != 0)
            {
                #region Set the vertex buffer and render states for drawing GUI
                vertexBuffer = (VertexBuffer)(vertexBufferList[0]);

#if FRB_MDX
                verts = vertexBuffer.Lock(0,
                     System.Math.Min(6000, mVerticesCounted) * VertexPositionColorTexture.StrideSize,
                    LockFlags.None); // Lock the buffer
                //			device.VertexFormat = CustomVertex.PositionNormalTextured.Format;

                #region Set the Device and renderer renderstates


                //			else
                //			{
                TextureFilter oldTextureFilter = FlatRedBallServices.GraphicsOptions.TextureFilter;

                Renderer.GraphicsDevice.SamplerState[0].MagFilter = TextureFilter.Point;
                Renderer.GraphicsDevice.SamplerState[0].MinFilter = TextureFilter.Point;
                //			}

                Renderer.GraphicsDevice.TextureState[0].AlphaArgument1 = TextureArgument.TextureColor;
                Renderer.GraphicsDevice.TextureState[0].AlphaArgument2 = TextureArgument.Diffuse;

                Renderer.GraphicsDevice.TextureState[0].AlphaOperation = TextureOperation.Modulate;

                Renderer.ColorOperation = Microsoft.DirectX.Direct3D.TextureOperation.Add;

                Renderer.GraphicsDevice.TextureState[0].ColorArgument1 = TextureArgument.TextureColor;
                Renderer.GraphicsDevice.TextureState[0].ColorArgument2 = TextureArgument.Diffuse;
                #endregion
#else
                Renderer.ColorOperation = ColorOperation.Texture;
#endif
                #endregion

                #region draw the regular windows
                for (int i = 0; i < mWindowArray.Count; i++)
                {
                    if (mWindowArray[i] is Window)
                    {
                        Window w = mWindowArray[i] as Window;
                        if (w.Visible && w.GuiManagerDrawn)
                        {
                            w.DrawSelfAndChildren(Camera);
                            w.DrawFloatingWindows(Camera);
                        }
                    }
                }
                #endregion

                #region draw the dominant window (window on top)

                foreach (Window w in mDominantWindows)
                {
                    if (w.Visible && w.GuiManagerDrawn)
                    {
                        w.DrawSelfAndChildren(Camera);
                        w.DrawFloatingWindows(Camera);
                    }

                }
                #endregion

                #region draw the perishable windows
                foreach (Window w in mPerishableArray)
                {
                    if (w.Visible && w.GuiManagerDrawn)
                    {
                        w.DrawSelfAndChildren(Camera);
                        w.DrawFloatingWindows(Camera);
                    }

                }
                #endregion

                #region Draw the Tool Tip

                if (string.IsNullOrEmpty(stringToWrite) == false && ShowingCursorTextBox == true)
                {

                    float width = TextManager.GetWidth(stringToWrite, GuiManager.TextSpacing);

                    if (width != 0)
                    {
                        #region Draw the tool tip background
                        mCursorTextBackground.ScaleX = width / 2.0f + 1f;

                        int numberOfLines = StringFunctions.GetLineCount(stringToWrite);

                        mCursorTextBackground.ScaleY = numberOfLines + .4f;

                        mCursorTextBackground.X = GuiManager.UnmodifiedXEdge + Cursor.XForUI;

                        mCursorTextBackground.X = System.Math.Max(mCursorTextBackground.X + .01f, mCursorTextBackground.ScaleX);
                        mCursorTextBackground.X =
                            System.Math.Min(mCursorTextBackground.X + .01f, 2 * GuiManager.XEdge - mCursorTextBackground.ScaleX);


                        mCursorTextBackground.Y = GuiManager.UnmodifiedYEdge - Cursor.YForUI - 2.5f - (numberOfLines - 1);



                        mCursorTextBackground.DrawSelfAndChildren(Camera);
                        #endregion

                        #region Draw the tool tip text

                        width += 1;
                        float textYPos = (float)(Cursor.YForUI) + 2.5f + 2 * (numberOfLines - 1);

                        TextManager.mXForVertexBuffer = mCursorTextBackground.X - GuiManager.UnmodifiedXEdge;
                        TextManager.mYForVertexBuffer = textYPos;
                        TextManager.mZForVertexBuffer = (float)Camera.Z + (100 * FlatRedBall.Math.MathFunctions.ForwardVector3.Z);
                        TextManager.mAlignmentForVertexBuffer = HorizontalAlignment.Center;


                        TextManager.mAlphaForVertexBuffer = GraphicalEnumerations.MaxColorComponentValue;
                        TextManager.mRedForVertexBuffer = 0;
                        TextManager.mGreenForVertexBuffer = 0;
                        TextManager.mBlueForVertexBuffer = 0;

                        TextManager.Draw(ref stringToWrite);
                        #endregion

                    }
                }
                #endregion

#if FRB_MDX
                Renderer.Texture = guiTexture.texture;
                vertexBuffer.Unlock();
#else
                Renderer.Texture = guiTexture;
#endif

#if !XNA4
                drawToOnThisVB = 0;
                drawnOnThisVB = 0;
                totalDrawn = 0;

                textureChange = 0;

                VBOn = 0;
#endif


#if XNA4
                throw new NotImplementedException();

#else
#if FRB_MDX
                Renderer.GraphicsDevice.SetStreamSource(0, (VertexBuffer)(vertexBufferList[VBOn]), 0);
                Renderer.GraphicsDevice.Transform.World = Matrix.Identity;
                Renderer.GraphicsDevice.VertexFormat = VertexPositionColorTexture.Format;
#else
                Renderer.GraphicsDevice.Vertices[0].SetSource(vertexBufferList[VBOn], 0, VertexPositionColorTexture.SizeInBytes);


#endif

                numPasses = 0;

                numTrianglesToDraw = 2000 * GuiManager.VertexBufferNumber + GuiManager.mNumberVerticesToDraw / 3;
                #region move through all of the VBs and draw the contents

                while (totalDrawn < numTrianglesToDraw)
                {
                    numPasses++;
                    drawToOnThisVB =  System.Math.Min(2000, numTrianglesToDraw - (2000 * VBOn));


                    if (textureChange < mTextureSwitches.Count && mTextureSwitches[textureChange].Index < 2000 * VBOn + drawToOnThisVB)
                    {
                        if (drawnOnThisVB < mTextureSwitches[textureChange].Index - 2 * (1000 * VBOn))
                        {
                            drawToOnThisVB = mTextureSwitches[textureChange].Index - 2 * (1000 * VBOn);

#if FRB_MDX
                            Renderer.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 3 * drawnOnThisVB, drawToOnThisVB - drawnOnThisVB);
#elif XNA4
                            throw new NotImplementedException();
#else
                            Renderer.Effect.Begin();

                            for (int i = 0; i < Renderer.Effect.CurrentTechnique.Passes.Count; i++)
                            {
                                EffectPass pass = Renderer.Effect.CurrentTechnique.Passes[i];
                                pass.Begin();

                                Renderer.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 3 * drawnOnThisVB, drawToOnThisVB - drawnOnThisVB);

                                pass.End();
                            }
                            Renderer.Effect.End();

#endif
                        }
                        else
                        {
                            drawToOnThisVB = mTextureSwitches[textureChange].Index - 2 * (1000 * VBOn) + 2;

#if FRB_MDX
                            Renderer.Texture = mTextureSwitches[textureChange].Texture.texture;
                            Renderer.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 3 * drawnOnThisVB, drawToOnThisVB - drawnOnThisVB);


                            if (mTextureSwitches[textureChange].SwitchBackAfterPrimitive)
                            {
                                Renderer.Texture = guiTexture.texture;
                            }
#else
                            Renderer.Texture = mTextureSwitches[textureChange].Texture;


#if XNA4
                            throw new NotImplementedException();
#else
                            Renderer.Effect.Begin();

                            for (int i = 0; i < Renderer.Effect.CurrentTechnique.Passes.Count; i++)
                            {
                                EffectPass pass = Renderer.Effect.CurrentTechnique.Passes[i];
                                pass.Begin();

                                Renderer.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 3 * drawnOnThisVB, drawToOnThisVB - drawnOnThisVB);
                                pass.End();
                            }
                            Renderer.Effect.End();
#endif
                            if (mTextureSwitches[textureChange].SwitchBackAfterPrimitive)
                            {
                                Renderer.Texture = guiTexture;
                            }

#endif

                            textureChange++;
                        }
                    }
                    else
                    {
#if FRB_MDX
                        Renderer.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 3 * drawnOnThisVB, drawToOnThisVB - drawnOnThisVB);
#else

#if XNA4
                        throw new NotImplementedException();
#else

                        Renderer.Effect.Begin();

                        for (int i = 0; i < Renderer.Effect.CurrentTechnique.Passes.Count; i++)
                        {
                            EffectPass pass = Renderer.Effect.CurrentTechnique.Passes[i];
                            pass.Begin();

                            Renderer.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 3 * drawnOnThisVB, drawToOnThisVB - drawnOnThisVB);
                            pass.End();
                        }
                        Renderer.Effect.End();
#endif
#endif

                    }

                    totalDrawn += drawToOnThisVB - drawnOnThisVB;

                    drawnOnThisVB = drawToOnThisVB;

                    if (drawToOnThisVB == 2000)
                    {
                        VBOn++;
                        drawnOnThisVB = 0;
#if FRB_MDX
                        Renderer.GraphicsDevice.SetStreamSource(0, (VertexBuffer)(vertexBufferList[VBOn]), 0);

#else
#if XNA4
            throw new NotImplementedException();
#else
                        Renderer.GraphicsDevice.Vertices[0].SetSource(vertexBufferList[VBOn], 0, VertexPositionColorTexture.SizeInBytes);
#endif

#endif
                    }
                }
                #endregion
#endif
#if FRB_MDX
                Renderer.GraphicsDevice.SetStreamSource(0, SpriteManager.vertexBuffer, 0);
                Renderer.GraphicsDevice.VertexFormat = CustomVertex.PositionNormalTextured.Format;

                Renderer.GraphicsDevice.SamplerState[0].MagFilter = oldTextureFilter;
                Renderer.GraphicsDevice.SamplerState[0].MinFilter = oldTextureFilter;


                Renderer.GraphicsDevice.TextureState[0].AlphaArgument1 = TextureArgument.TextureColor;
                Renderer.GraphicsDevice.TextureState[0].AlphaArgument2 = TextureArgument.TFactor;

                Renderer.ColorOperation = Microsoft.DirectX.Direct3D.TextureOperation.SelectArg1;
                Renderer.GraphicsDevice.TextureState[0].ColorArgument2 = TextureArgument.TFactor;
#endif

            }



            #region If a game-drawn cursor is used, draw it

            if (mCursors[0].si.Visible)
            {
                if (FlatRedBallServices.IsWindowsCursorVisible == false)
                {
#if FRB_MDX
                    Renderer.GraphicsDevice.SetStreamSource(0, SpriteManager.vertexBuffer, 0);
                    Renderer.GraphicsDevice.VertexFormat = CustomVertex.PositionNormalTextured.Format;

                    Renderer.GraphicsDevice.Transform.Projection = Matrix.OrthoLH(mCursors[0].mCamera.XEdge * 2, mCursors[0].mCamera.YEdge * 2, 85, 110);
                    Renderer.GraphicsDevice.Transform.View = Matrix.LookAtLH(
                        new Vector3(mCursors[0].mCamera.X, mCursors[0].mCamera.Y, mCursors[0].mCamera.Z),
                        new Vector3(mCursors[0].mCamera.X, mCursors[0].mCamera.Y, mCursors[0].mCamera.Z + 1),
                        new Vector3(0,0,1));

                    DrawCursor(mCursors[0]);
#else

                    float xPosition = mCursors[0].mCamera.DestinationRectangle.Width / 2.0f +
                        mCursors[0].mCamera.DestinationRectangle.Width / 2.0f * (mCursors[0].XForUI) / mCursors[0].mCamera.XEdge;

                    float yPosition = mCursors[0].mCamera.DestinationRectangle.Height / 2.0f -
                        mCursors[0].mCamera.DestinationRectangle.Height / 2.0f * (mCursors[0].YForUI) / mCursors[0].mCamera.YEdge;

#if WINDOWS_PHONE || MONODROID

                    // don't do anything since the cursor always shows on the PC emulator, and shouldn't show on the phone
#elif XNA4

                    throw new NotImplementedException();
#else
                    // Draw the sprite.
                    mSpriteBatch.Begin(SpriteBlendMode.AlphaBlend);


					Texture2D texture = GuiManager.guiTexture;

					Microsoft.Xna.Framework.Rectangle rectangle =
						new Microsoft.Xna.Framework.Rectangle(118, 166, 15, 18);

					if (mCursors[0].si.Texture != null)
					{
						texture = mCursors[0].si.Texture;
						// currently we only support fullscreen custom textures for cursors.  Vic, change this
						// if someone on the forums doesn't like it.
						rectangle =
							new Microsoft.Xna.Framework.Rectangle(0, 0, texture.Width, texture.Height);
					}


					mSpriteBatch.Draw(texture,
                        new Vector2(xPosition, yPosition), 
                        rectangle, Color.White);
                    mSpriteBatch.End();
#endif
#endif
                }

            }
            #endregion

            Camera.NearClipPlane = oldNew;
            Camera.FarClipPlane = oldFar;
            Camera.UpVector = oldCameraUp;

#if FRB_MDX
            Renderer.GraphicsDevice.Transform.Projection = Matrix.PerspectiveFovLH(Camera.FieldOfView, Camera.AspectRatio, .20f, 720.0f);
            Camera.FieldOfView = oldFieldOfView;
#endif


        }// end of void Draw() method	

        static internal void DrawCursor(Cursor cursorToDraw)
        {
#if FRB_MDX
            if (cursorToDraw.Visible)
            {
                Renderer.GraphicsDevice.VertexFormat = CustomVertex.PositionNormalTextured.Format;
                Renderer.GraphicsDevice.TextureState[0].AlphaArgument1 = TextureArgument.TextureColor;
                Renderer.GraphicsDevice.TextureState[0].AlphaArgument2 = TextureArgument.TFactor;

                Renderer.ColorOperation = Microsoft.DirectX.Direct3D.TextureOperation.SelectArg1;
                Renderer.GraphicsDevice.TextureState[0].ColorArgument2 = TextureArgument.TFactor;


                Renderer.GraphicsDevice.Transform.World =
                    Matrix.Scaling(cursorToDraw.si.ScaleX, cursorToDraw.si.ScaleY, 1.0f);
                Renderer.GraphicsDevice.Transform.World *=
                    Matrix.RotationYawPitchRoll(cursorToDraw.si.RotationX, cursorToDraw.si.RotationY, cursorToDraw.si.RotationZ);
                Renderer.GraphicsDevice.Transform.World *=
                    Matrix.Translation(cursorToDraw.si.X + cursorToDraw.mCamera.X, cursorToDraw.si.Y + cursorToDraw.mCamera.Y,
                    Cursor.mCamera.Z + 100);

                Renderer.Texture = cursorToDraw.si.mTexture.texture;

                Renderer.GraphicsDevice.RenderState.TextureFactor = Color.FromArgb((int)(cursorToDraw.si.Alpha),
                    0, 0, 0).ToArgb();
                //				device.SetStreamSource(0, vertexBuffer, 0);
                Renderer.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }
#else
            throw new NotImplementedException("Draw cursor not implemented");
#endif
        }

        static internal void DrawCursor(Camera cursorsCamera)
        {
#if FRB_MDX
            Renderer.GraphicsDevice.SetStreamSource(0, SpriteManager.vertexBuffer, 0);
            Renderer.GraphicsDevice.VertexFormat = CustomVertex.PositionNormalTextured.Format;

            Renderer.GraphicsDevice.Transform.Projection = Matrix.OrthoLH(cursorsCamera.XEdge * 2, cursorsCamera.YEdge * 2, 85, 110);
            Renderer.GraphicsDevice.Transform.View = Matrix.LookAtLH(
                new Vector3(cursorsCamera.X, cursorsCamera.Y, cursorsCamera.Z), new Vector3(cursorsCamera.X, cursorsCamera.Y, cursorsCamera.Z + 1), 
                    new Vector3(0,1,0));


            foreach (Cursor c in mCursors)
                if (c.mCamera == cursorsCamera)
                    DrawCursor(c);
#else
            throw new NotImplementedException("Draw Cursor Not Implemented");
#endif
        }





        static internal void ReactToResizing()
        {
            // It's possible that the GuiManager hasn't been created yet
            if (mWindowArray == null)
            {
                return;
            }


            foreach (IWindow window in mWindowArray)
            {
                if (window is MenuStrip)
                {
                    ((MenuStrip)window).SetScaleTL(GuiManager.XEdge, window.ScaleY);

                }
                else if (window is InfoBar)
                {
                    window.SetScaleTL(Camera.XEdge, window.ScaleY);

                    window.Y = 2 * Camera.YEdge - window.ScaleY;
                }

            }

        }

        private static void SetPropertyGridTypeAssociations()
        {
#if !SILVERLIGHT && !WINDOWS_PHONE && !MONODROID

            PropertyGrid.SetPropertyGridTypeAssociation(typeof(string), typeof(StringPropertyGrid));
            PropertyGrid.SetPropertyGridTypeAssociation(typeof(int), typeof(IntPropertyGrid));
            PropertyGrid.SetPropertyGridTypeAssociation(typeof(float), typeof(FloatPropertyGrid));
            PropertyGrid.SetPropertyGridTypeAssociation(typeof(double), typeof(DoublePropertyGrid));
            PropertyGrid.SetPropertyGridTypeAssociation(typeof(long), typeof(LongPropertyGrid));
            PropertyGrid.SetPropertyGridTypeAssociation(typeof(bool), typeof(BoolPropertyGrid));
#endif

        }

        static ObjectDisplayManager mObjectDisplayManager = new ObjectDisplayManager();

        public static ObjectDisplayManager ObjectDisplayManager
        {
            get
            {
                return mObjectDisplayManager;
            }
        }

        static void CheckVBIncrement()
        {
            if (mNumberVerticesToDraw == 6000)
            {
                mNumberVerticesToDraw = 0;
#if FRB_MDX
                vertexBuffer.Unlock();
                VertexBufferNumber++;
                vertexBuffer = (VertexBuffer)(vertexBufferList[VertexBufferNumber]);

                verts = vertexBuffer.Lock(0,
                    6000 * VertexPositionColorTexture.StrideSize,
                    LockFlags.None); // Lock the buffer (which will return our structs)
#else
                // grab the next VertexBuffer
                VertexBufferNumber++;
                vertexBuffer = vertexBufferList[VertexBufferNumber];

#endif
            }
        }

        static void LoseInputOnTextBox(TextBox tempTextBox)
        {
            #region if lost input and the former text box was a DECIMAL/INTEGER, set value to 0 if empty.  If UpDown, set value between bounds
            if (tempTextBox != null && tempTextBox != InputManager.ReceivingInput)
            { // if we have lost input
                if (tempTextBox.Text == "" && (tempTextBox.Format == TextBox.FormatTypes.Decimal || tempTextBox.Format == TextBox.FormatTypes.Integer))
                {
                    tempTextBox.Text = "0";
                }
                if (tempTextBox.Parent != null && tempTextBox.Parent is FlatRedBall.Gui.UpDown)
                {
                    ((UpDown)tempTextBox.Parent).CurrentValue = float.Parse(tempTextBox.Text);
                    if (((UpDown)tempTextBox.Parent).CurrentValue > ((UpDown)tempTextBox.Parent).MaxValue)
                        ((UpDown)tempTextBox.Parent).CurrentValue = ((UpDown)tempTextBox.Parent).MaxValue;
                    else if (((UpDown)tempTextBox.Parent).CurrentValue < ((UpDown)tempTextBox.Parent).MinValue)
                        ((UpDown)tempTextBox.Parent).CurrentValue = ((UpDown)tempTextBox.Parent).MinValue;
                }
            }
            #endregion

        }
    }
}
