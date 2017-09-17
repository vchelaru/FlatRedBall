
using System;
using System.IO;
using System.Text;
using System.Linq;

using System.Collections;
using System.Globalization;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Vector3 = Microsoft.Xna.Framework.Vector3;

using FlatRedBall.Input;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics;
using FlatRedBall.Utilities;
using System.Collections.ObjectModel;

using Color = Microsoft.Xna.Framework.Color;
using FlatRedBall.IO;


namespace FlatRedBall.Gui
{
    #region Classes



    public static class EnumerableExtensionMethods
    {
        public static bool Contains(this IEnumerable<IWindow> enumerable, IWindow item)
        {
            foreach (var window in enumerable)
            {
                if (window == item)
                {
                    return true;
                }
            }
            return false;
        }
    }

    #endregion

    #region Enums
    public enum Sides
    {
        None = 0,
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8,
        TopLeft = Top | Left,
        TopRight = Top | Right,
        BottomRight = Bottom | Right,
        BottomLeft = Bottom | Left

    }
    #endregion


    public delegate void WindowEvent(IWindow window);

	public static partial class GuiManager
	{

		#region Enums

		public enum GuiControl
		{
			Mouse = 0,
			Joystick = 1
		}

        public enum VisibilityPreservation
        {
            PreserveVisibility,
            OverwriteVisibility
        }

		#endregion



		#region Fields


        static bool mUIEnabled;
        public const string InternalGuiContentManagerName = "FlatRedBall Internal GUI";

		static WindowArray mWindowArray;
        static ReadOnlyCollection<IWindow> mReadOnlyWindowArray;

        // Perishable windows are windows which exist until
        // the user clicks.  This is common for ComboBoxes and MenuStrips.
        // When a Perishable Window is removed from the GuiManager it is both
        // made invisible as well as removed.  This can help parent windows of 
        // Perishable Windows know when a Perishable Window has perished.
		static WindowArray mPerishableArray = new WindowArray();
        static ReadOnlyCollection<IWindow> mPerishableArrayReadOnly;

		/// <summary>
		/// A stack of Windows which demand input from the cursor.
		/// </summary>
		/// <remarks>
        /// When a dominantWindow is valid, the cursor will not be able to interact with other windows.  If RemoveInvisibleDominantWindows
        /// is set to true (default is true) then the GuiManager will remove any invisible dominant windows from its
        /// memory.
        /// In other words a DominantWindow can be removed either through the traditional Remove methods or by
        /// setting the Window's Visible property to false if RemoveInvisibleDominantWindows is true.
		/// </remarks>
        static List<IWindow> mDominantWindows;

        static List<Cursor> mCursors;


		//static bool mReceivingInputJustSet;// = false;

        // This variable keeps track of the last UI element
        // which had focus (was clicked on).  Whenever the user
        // clicks on a UI element, it is compared to the mLastWindowWithFocus.
        // If the window clicked on does not match mLastWindowWithFocus, the
        // mLastWindowWithFocus's callOnLosingFocus gets called.
		internal static IWindow mLastWindowWithFocus;// = null;

        // Controls whether the tool tip is shown
		static public bool ShowingCursorTextBox;

		static public System.Globalization.NumberFormatInfo nfi;



		static public float TextHeight;
		static public float TextSpacing;




        public static List<String> renderingNotes;



        private static float mOverridingFieldOfView = float.NaN;

        #region XML Docs
        /// <summary>
        /// Sets the tool tip to show
        /// </summary>
        /// <remarks>
        /// Some UI elements like Buttons automatically show
        /// a tool tip.  This property can be used to overwrite
        /// what is shown, or to show tool tips when the user is
        /// over a non-UI element.
        /// </remarks>
        #endregion
        private static string mToolTipText;

        static WindowArray mPerishableWindowsToSurviveClick = new WindowArray();

		#endregion



		#region Properties

        public static bool RemoveInvisibleDominantWindows
        {
            get;
            set;
        }

        [Obsolete("Set the native cursor visibility using FlatRedBallServices.Game.IsMouseVisible")]
		public static bool DrawCursorEvenIfThereIsNoUI
		{
			get;
			set;
		}

        static public bool DominantWindowActive
		{
			get
			{
                foreach (IWindow window in mDominantWindows)
                {
                    if (window.AbsoluteVisible)
                    {
                        return true;
                    }
                }
                return false;
                // July 29, 2012
                // Now the GuiManager
                // can handle invisible
                // dominant windows.  Therefore
                // we have to see what is visible.
//                return mDominantWindows.Count != 0;
			}
		}

        public static Cursor Cursor
        {
            get 
            {
                try
                {
                    return mCursors[0];
                }
                catch
                {
                    throw new InvalidOperationException(
                        "There are no Cursors created yet - has FlatRedBall been initialized?");
                }

            }
        }


        public static List<Cursor> Cursors
        {
            get { return mCursors; }
        }

        public static Camera Camera
        {
            get { return SpriteManager.Camera; }
        }

        public static bool IsUIEnabled
        {
            get{ return mUIEnabled;}
            set{ mUIEnabled = value;}
        }



        public static float OverridingFieldOfView
        {
            get { return mOverridingFieldOfView; }
            set { mOverridingFieldOfView = value; }
        }

        public static ReadOnlyCollection<IWindow> PerishableWindows
        {
            get { return mPerishableArrayReadOnly; }
        }

        public static IEnumerable<IWindow> DominantWindows
        {
            get { return mDominantWindows; }
        }

        public static string ToolTipText
        {
            get { return mToolTipText; }
            set { mToolTipText = value; }
        }

#if !SILVERLIGHT

        public static ReadOnlyCollection<IWindow> Windows
        {
            get { return mReadOnlyWindowArray; }
        }

#endif

        public static float XEdge
        {
            get
            {
                return YEdge * Camera.AspectRatio;
            }
        }

        public static float YEdge
        {
            get 
            {
                if (!float.IsNaN(mOverridingFieldOfView))
                {
                    return (float)(100 * System.Math.Tan(mOverridingFieldOfView / 2.0));
                }
                else
                {
                    return Camera.YEdge;
                }
            }
        }


		public static bool BringsClickedWindowsToFront { get; set; }

        public static float UnmodifiedXEdge
        {
            get
            {
                return UnmodifiedYEdge * 4 / 3.0f;
            }
        }

        public static float UnmodifiedYEdge
        {
            get
            {
                return (float)(100 * System.Math.Tan((System.Math.PI / 4.0f) / 2.0));
            }
        }

        #endregion


	
        #region Methods

        #region Constructors

#if FRB_MDX
        internal static void Initialize(string guiTextureToUse, System.Windows.Forms.Control form)
		{
            Initialize(guiTextureToUse, form, new Cursor(SpriteManager.Camera, form));
        
        }


		internal static void Initialize(string guiTextureToUse, System.Windows.Forms.Control form, Cursor cursor)
#else
        // made public for unit tests
		public static void Initialize(Texture2D guiTextureToUse, Cursor cursor)

#endif
		{
#if FRB_MDX || XNA3_1
            RemoveInvisibleDominantWindows = true;

#else
            RemoveInvisibleDominantWindows = false;
#endif
            mPerishableArrayReadOnly = new ReadOnlyCollection<IWindow>(mPerishableArray);
            // Currently make the FRB XNA default to not using the UI, but the FRB MDX to true
            TextHeight = 2;
            TextSpacing = 1;

            mUIEnabled = true;



            //		sr.WriteLine("Inside the GuiManager constructor");
            //		sr.Close();
            mCursors = new List<Cursor>();

            mCursors.Add(cursor);

            mWindowArray = new WindowArray();
            mReadOnlyWindowArray = new ReadOnlyCollection<IWindow>(mWindowArray);

            mDominantWindows = new List<IWindow>();

#if !MONOGAME && !SILVERLIGHT && !UNIT_TESTS && !XNA4
            RenderingBasedInitializize();
#endif

            BringsClickedWindowsToFront = true;


            try
            {
#if FRB_MDX
                if (System.IO.File.Exists(FlatRedBall.IO.FileManager.RelativeDirectory + "Assets/Textures/upDirectory.bmp"))
                {

                    mUpDirectory = FlatRedBallServices.Load<Texture2D>(
                        FlatRedBall.IO.FileManager.RelativeDirectory + "Assets/Textures/upDirectory.bmp", 
                        InternalGuiContentManagerName);
                }
                if (System.IO.File.Exists(FlatRedBall.IO.FileManager.RelativeDirectory + "Assets/Textures/cursorTextBox.bmp"))
                {
                    mCursorTextBox = FlatRedBallServices.Load<Texture2D>(
                        FlatRedBall.IO.FileManager.RelativeDirectory + "Assets/Textures/cursorTextBox.bmp",
                        InternalGuiContentManagerName);
                }




                if (guiTextureToUse != null && guiTextureToUse != "")
                {
                    guiTexture = FlatRedBallServices.Load<Texture2D>(
                        guiTextureToUse, InternalGuiContentManagerName);

                    RefreshTextSize();
                }

#elif SUPPORTS_FRB_DRAWN_GUI
                guiTexture = guiTextureToUse;

                RefreshTextSize();
#endif

            }
            catch(Exception e)
            {
                throw e;
            }
            try
            {

                nfi = new System.Globalization.NumberFormatInfo();
                //replaced the above line with the one below to used streamed images.


                ShowingCursorTextBox = true;

                renderingNotes = new List<String>();
            }
            catch(Exception e)
            {
                throw e;
            }

#if SUPPORTS_FRB_DRAWN_GUI
            SetPropertyGridTypeAssociations();
#endif

            // Let's do some updates because we want to make sure our "last" values are set to the current value
            // so we don't have any movement on the cursor initially:
            cursor.Update(TimeManager.CurrentTime);
            cursor.Update(TimeManager.CurrentTime);

        }

        #endregion


        #region Public Methods


		#region adding gui component methods
		

        public static void AddWindow(IWindow windowToAdd)
        {
#if DEBUG
            if (windowToAdd == null)
            {
                throw new ArgumentException("Argument Window can't be null");
            }
            if (mWindowArray.Contains(windowToAdd))
            {
                int index = mWindowArray.IndexOf(windowToAdd);

                throw new ArgumentException("This window has already been added to the GuiManager.  It is at index " + index);
            }
            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Windows can only be added on the primary thread");
            }
#endif
            mWindowArray.Add(windowToAdd);

            if (BringsClickedWindowsToFront == false)
            {
                InsertionSort(mWindowArray, WindowComparisonForSorting);
            }
        }
        
		#endregion

        internal static float GetYOffsetForModifiedAspectRatio()
        {
            // make 0,0 the top-left
            float unmodifiedYEdge = UnmodifiedYEdge;

            float offset = unmodifiedYEdge - GuiManager.YEdge;
            return offset;
        }

        internal static float GetXOffsetForModifiedAspectRatio()
        {
            float unmodifiedXEdge = UnmodifiedXEdge;

            return unmodifiedXEdge - GuiManager.XEdge;
        }

#if !XBOX360 && !SILVERLIGHT && !WINDOWS_PHONE && !MONOGAME && !WINDOWS_8

        static public Cursor AddCursor(Camera camera, System.Windows.Forms.Form form)
        {
#if FRB_MDX
            Cursor cursorToAdd = new Cursor(camera, form);
#else
            Cursor cursorToAdd = new Cursor(camera);

#endif
            cursorToAdd.SetCursor(
                FlatRedBallServices.Load<Texture2D>("Assets/Textures/cursor1.bmp", InternalGuiContentManagerName), -.5f, 1);

            mCursors.Add(cursorToAdd);
            return cursorToAdd;

        }
#endif

		#region XML Docs
		/// <summary>
        /// Adds a window as a Dominant Window.  If the window is a regular Window
        /// already managed by the GuiManager it will be removed from the regularly-managed
        /// windows.
        /// </summary>
        /// <param name="windowToSet">The window to add to the Dominant Window stack.</param>
        #endregion
        static public void AddDominantWindow(IWindow window)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Dominant windows can only be added or modified on the primary thread");
            }
#endif
            // Let's make these tolerant
            if (mWindowArray.Contains(window))
            {
                mWindowArray.Remove(window);
            }

            if (!mDominantWindows.Contains(window))
            {
                mDominantWindows.Add(window);
            }
        }

#if !SILVERLIGHT




        public static void BringToFront(IWindow windowToBringToFront)
        {
            if (windowToBringToFront.Parent == null)
            {

                if (mWindowArray.Contains(windowToBringToFront) == false)
                    return;

                mWindowArray.Remove(windowToBringToFront); mWindowArray.Add(windowToBringToFront);
            }
            else
            {
#if SUPPORTS_FRB_DRAWN_GUI
                Window parentwindow = (Window)windowToBringToFront.Parent;

                parentwindow.BringToFront(windowToBringToFront);
#endif
            }
        }

        public static void SendToBack(IWindow windowToSendToBack)
        {
            if (windowToSendToBack.Parent == null)
            {
                if (mWindowArray.Contains(windowToSendToBack) == true)
                {
                    mWindowArray.Remove(windowToSendToBack);
                    mWindowArray.Insert(0, windowToSendToBack);
                }
            }
            else
            {
                throw new NotImplementedException("Send to back not implemented for parents at this time.");
            }
        }

#endif

        static public void ElementActivity()
        {
            foreach (IWindow w in mWindowArray)
            {
                w.Activity(Camera);
            }

            foreach (IWindow w in mPerishableArray)
            {
                w.Activity(Camera);
            }

            foreach (IWindow w in mDominantWindows)
            {
                w.Activity(Camera);
            }
        }

#if !SILVERLIGHT && !WINDOWS_PHONE && !MONODROID




        static public Cursor GetCursorNum(int index)
        {
            if (index > -1 && index < mCursors.Count)
                return mCursors[index];
            else
                return null;
        }
#endif


#if !SILVERLIGHT && !WINDOWS_PHONE && !MONOGAME



        static public void LoadSettingsFromText(string settingsTextFile)
        {
            TextReader tr = new StreamReader(settingsTextFile);
            string buffer = tr.ReadToEnd();

            float cursorSensitivity = StringFunctions.GetFloatAfter("Mouse Sensitivity: ", buffer);
            if (!float.IsNaN(cursorSensitivity))
                Cursor.sensitivity = cursorSensitivity;

            FileManager.Close(tr);

        }

#endif

        [Obsolete("Use AddDominantWindow instead - this method will go away soon")]
        public static void MakeDominantWindow(IWindow window)
        {
            AddDominantWindow(window);
        }

        public static void MakeRegularWindow(IWindow window)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Windows can only be added or modified on the primary thread");
            }
#endif

            if (mDominantWindows.Contains(window))
            {
                mDominantWindows.Remove(window);
            }

            if (!mWindowArray.Contains(window))
            {
                mWindowArray.Add(window);
            }

        }

        /// <summary>
        /// Sorts all contained IWindows according to their
        /// Z values and Layers.  This will usually result in
        /// clicks being received in the same order that objects
        /// are drawn, which is what the user will usually expect
        /// </summary>
        public static void SortZAndLayerBased()
        {
            // This is not a stable sort. We need it to be:
            //mWindowArray.Sort(SortZAndLayerBased);
            InsertionSort(mWindowArray, WindowComparisonForSorting);
        }

        public static void InsertionSort<T>(IList<T> list, Comparison<T> comparison)
        {
            if (list == null)
                throw new ArgumentNullException("list");
            if (comparison == null)
                throw new ArgumentNullException("comparison");

            int count = list.Count;
            for (int j = 1; j < count; j++)
            {
                T key = list[j];

                int i = j - 1;
                for (; i >= 0 && comparison(list[i], key) > 0; i--)
                {
                    list[i + 1] = list[i];
                }
                list[i + 1] = key;
            }
        }

        static int WindowComparisonForSorting(IWindow first, IWindow second)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif

            if (first.Layer == second.Layer)
            {
                if (first.Z == second.Z)
                {
                    return mWindowArray.IndexOf(first).CompareTo(mWindowArray.IndexOf(second));
                }
                else
                {
                    return first.Z.CompareTo(second.Z);
                }
            }
            else
            {
                int firstLayerIndex = SpriteManager.LayersWriteable.IndexOf(first.Layer);
                int secondLayerIndex = SpriteManager.LayersWriteable.IndexOf(second.Layer);
                return firstLayerIndex.CompareTo(secondLayerIndex);
            }
        }


#if !SILVERLIGHT && !WINDOWS_PHONE && !MONODROID


        public static new string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("Number of regular Windows ").Append(mWindowArray.Count);

            return stringBuilder.ToString();
        }

#endif
        static public void UpdateDependencies()
        {
            // This thing is a linked list, so we gotta foreach it
            foreach (IWindow w in mDominantWindows)
                w.UpdateDependencies();
            for (int i = 0; i < mWindowArray.Count; i++)
                mWindowArray[i].UpdateDependencies();
            for (int i = 0; i < mPerishableArray.Count; i++)
                mPerishableArray[i].UpdateDependencies();


        }

        public static void WorldToUi(float worldX, float worldY, float worldZ, out float uiX, out float uiY)
        {
            uiX = worldX;
            uiY = worldY;

            uiX = uiX - SpriteManager.Camera.X;
            uiY -= SpriteManager.Camera.Y;

            uiX *= SpriteManager.Camera.XEdge / SpriteManager.Camera.RelativeXEdgeAt(worldZ);
            uiY *= -SpriteManager.Camera.YEdge / SpriteManager.Camera.RelativeYEdgeAt(worldZ);

            uiX += SpriteManager.Camera.XEdge;
            uiY += SpriteManager.Camera.YEdge;
        }

        #endregion


        #region Internal
        
        public static void Control()
        {
#if PROFILE
            TimeManager.TimeSection("Object Display Manager Activity");
#endif

            InputManager.ReceivingInputJustSet = false;

            mToolTipText = "";

            #region Update cursors

            foreach (Cursor c in mCursors)
            {

                #region SET CURSOR WINDOWPUSHED and WindowMiddleButtonPushed TO NULL if we push or click on something that is not a window
                // Do this before the activity so on a click the frame with the click will still have a valid
                // WindowPushed.
                if ((c.PrimaryPush && c.WindowOver == null) || c.PrimaryClick)
                {
                    if (c.WindowPushed != null)
                    {
                        c.WindowPushed = null;
                    }
                }

                #endregion

                c.Update(TimeManager.CurrentTime);
                c.WindowOver = null;
                c.WindowClosing = null;

            }

            #endregion

            UpdateDependencies();

            // now we find which button we are over with each cursor

            ElementActivity();

            #region Loop through cursors and perform collision and action vs. Windows
            foreach (Cursor c in mCursors)
            {


                if (c.Active)
                {
                    #region looping through all perishable windows
                    for (int i = mPerishableArray.Count - 1; i > -1; i--)
                    {

                        IWindow window = mPerishableArray[i];

                        if (c.IsOn(window))
                        {
                            window.TestCollision(c);
                        }
                        else if (c.PrimaryClick || c.SecondaryClick)
                        {
                            if (mPerishableWindowsToSurviveClick.Contains(window))
                            {
                                mPerishableWindowsToSurviveClick.Remove(window);
                            }
                            else
                            {
                                window.Visible = false;
                                mPerishableArray.RemoveAt(i);
                            }
                        }
                    }

                    #endregion

                    #region if we have a dominant window
                    if (DominantWindowActive && c.WindowOver == null)
                    {
                        for(int i = mDominantWindows.Count - 1; i > -1; i--)
                        {
                            IWindow dominantWindow = mDominantWindows[i];

                            if (dominantWindow.Visible && 
                                c.IsOnWindowOrFloatingChildren(dominantWindow))
                            {
                                dominantWindow.TestCollision(c);
                            }
                        }
                        // If there are any dominant windows, we shouldn't perform any other collision tests
                        continue;
                    }
                    #endregion

                    #region looping through all regular windows

                        // First check all floating windows
                    else if (c.WindowOver == null)
                    {
                        for (int i = mWindowArray.Count - 1; i > -1; i--)
                        {
                            if (!mWindowArray[i].GuiManagerDrawn || mWindowArray[i].Visible == false || !mWindowArray[i].Enabled)
                                continue;

                            IWindow windowOver = c.GetDeepestFloatingChildWindowOver(mWindowArray[i]);
                            IWindow tempWindow = mWindowArray[i]; 

                            if (windowOver != null)
                            {
                                windowOver.TestCollision(c);

                                if (c.PrimaryPush && i < mWindowArray.Count)
                                {// we pushed a button, so let's bring it to the front
                                    mWindowArray.Remove(tempWindow); 
                                    mWindowArray.Add(tempWindow);
                                }
                                break;
                            }
                        }
                    }

                    if (c.WindowOver == null)
                    {
                        for (int i = mWindowArray.Count - 1; i > -1; i--)
                        {
                            // Code in this loop can call
                            // events.  Events may destroy
                            // entire groups of Windows, so we
                            // need to make sure we're still vaild:
                            if (i < mWindowArray.Count)
                            {
                                var window = mWindowArray[i];


                                if (window.Visible == false || !window.Enabled)
                                    continue;

                                if (!window.IgnoredByCursor && window.HasCursorOver(c))
                                {
                                    window.TestCollision(c);
                                    // I think we should use the cursor's WindowOver which may be a child of Window
                                    c.LastWindowOver = c.WindowOver;
                                    if (Cursor.PrimaryPush && i < mWindowArray.Count && BringsClickedWindowsToFront == true)
                                    {// we pushed a button, so let's bring it to the front
                                        mWindowArray.Remove(window); mWindowArray.Add(window);
                                    }
                                    break;

                                }
                            }
                        }
                    }
                    #endregion
                }
            }

            #endregion


            #region call onLosingFocus
            if (Cursor.PrimaryPush)// && guiResult.windowResult != null)
            {
                // Should this be on a per-cursor basis?
                if (mLastWindowWithFocus != null && mLastWindowWithFocus != Cursor.WindowOver)
                {
                    mLastWindowWithFocus.OnLosingFocus();
#if SUPPORTS_FRB_DRAWN_GUI
                    UpDownReactToCursorPush();
#endif
                }
                mLastWindowWithFocus = Cursor.WindowOver;

            }
            #endregion

            #region If not on anything, set cursor.LastWindowOver to null
            // Should this be on a per-cursor basis?
            if (Cursor.WindowOver == null)
                Cursor.LastWindowOver = null;
            #endregion

			#region if receivingInput setting and resetting logic
			foreach (Cursor c in mCursors)
            {
                if (c.Active && c.PrimaryClick == true)
                {
                    FlatRedBall.Gui.IInputReceiver objectClickedOn = c.WindowOver as IInputReceiver;

#if SUPPORTS_FRB_DRAWN_GUI
                    UpDownReactToPrimaryClick(c, ref objectClickedOn);
#endif

                    #region Check for ReceivingInput being set to null
                    if (InputManager.ReceivingInputJustSet == false && objectClickedOn == null)
                    {
#if SUPPORTS_FRB_DRAWN_GUI
                        bool shouldLoseInput = true;

                        if (InputManager.ReceivingInput != null && InputManager.ReceivingInput is TextBox &&
                            c.WindowPushed == InputManager.ReceivingInput)
                        {
                            shouldLoseInput = false;
                        }

                        if (shouldLoseInput)
                        {
                            InputManager.ReceivingInput = null;
                        }
#endif
                    }
                    #endregion
                    else if (objectClickedOn != null && ((IWindow)objectClickedOn).Visible == true &&
                        ((IInputReceiver)objectClickedOn).TakingInput)
                    {
                        InputManager.InputReceiver = objectClickedOn;
                    }
                }
            }
            #endregion

#if SUPPORTS_FRB_DRAWN_GUI
            LoseInputOnTextBox(tempTextBox);
#endif

            #region letting go of any grabbed window
            if (Cursor.PrimaryClick)
            {
                Cursor.mWindowGrabbed = null;
                Cursor.mSidesGrabbed = Sides.None;

            }
            #endregion

            #region regulate button and toggle button up/down states depending on if the cursor is still over the button pushed
            if (Cursor.WindowPushed != null && Cursor.WindowOver != Cursor.WindowPushed)
            {
#if SUPPORTS_FRB_DRAWN_GUI
                ButtonReactToPush();
#endif
            }

            #endregion

            #region Remove invisible Dominant Windows
            while (mDominantWindows.Count > 0 && mDominantWindows[mDominantWindows.Count - 1].Visible == false &&
                RemoveInvisibleDominantWindows)
            {
                mDominantWindows.RemoveAt(mDominantWindows.Count - 1);
            }
            #endregion

            #region Clear the mPerishableWindowsToSurviveClick
            mPerishableWindowsToSurviveClick.Clear();
            #endregion


        }


        #endregion


        #region Private Methods



        #endregion


        #region Remove methods

        static public void RemoveCursor(Cursor cursorToRemove)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Cursors can only be removed on the primary thread");
            }
#endif
            mCursors.Remove(cursorToRemove);
        }


        public static void RemoveParentOfWindow(IWindow childWindow)
        {
            RemoveWindow(childWindow.Parent);
        }


		public static void RemoveWindow(IWindow windowToRemove)
		{
            RemoveWindow(windowToRemove, false);
		}


        public static void RemoveWindow(IWindow windowToRemove, bool keepEvents)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Windows can only be removed on the primary thread");
            }
#endif
            if (mWindowArray.Contains(windowToRemove))
            {
                mWindowArray.Remove(windowToRemove);
            }
            else if (mPerishableArray.Contains(windowToRemove))
            {
                windowToRemove.Visible = false;
                mPerishableArray.Remove(windowToRemove);
            }
                // If an IWindow is made dominant, then it will be removed from the regular (mWindowArray) window list.
            else if (mDominantWindows.Contains(windowToRemove))
            {
                mDominantWindows.Remove(windowToRemove);
            }

            if (InputManager.InputReceiver == windowToRemove)
                InputManager.InputReceiver = null;
        }


        static public void RemoveWindow(WindowArray windowsToRemove)
        {
            for (int i = windowsToRemove.Count - 1; i > -1; i--)
            {
                RemoveWindow(windowsToRemove[i]);
            }
        }

        #endregion
	


		#endregion


	}// end of class GuiManager

    

}
