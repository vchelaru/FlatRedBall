
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

using FlatRedBall.Input;
using System.Collections.ObjectModel;


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

        static List<Action> nextPushActions = new List<Action>();
        static List<Action> nextClickActions = new List<Action>();

        static bool mUIEnabled;

        public const string InternalGuiContentManagerName = "FlatRedBall Internal GUI";

		static List<IWindow> mWindowArray;
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

        /// <summary>
        /// Gets the main cursor. If multiple cursors have been added to the GuiManager, returns the first one.
        /// </summary>
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

        public static Camera Camera => Camera.Main;

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

        public static IEnumerable<IWindow> DominantWindows => mDominantWindows; 

        public static string ToolTipText
        {
            get { return mToolTipText; }
            set { mToolTipText = value; }
        }

        public static ReadOnlyCollection<IWindow> Windows
        {
            get { return mReadOnlyWindowArray; }
        }

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

        /// <summary>
        /// The list of Xbox360GamePads used to control UI elements (tabbing and selecting)
        /// </summary>
        public static List<Xbox360GamePad> GamePadsForUiControl
        {
            get; private set;
        } = new List<Xbox360GamePad>();

        public static List<GenericGamePad> GenericGamePadsForUiControl
        {
            get; private set;
        } = new List<GenericGamePad>();

        #endregion

	
        #region Methods

        #region Constructors

        // made public for unit tests
		public static void Initialize(Texture2D guiTextureToUse, Cursor cursor)

		{
            RemoveInvisibleDominantWindows = false;

            mPerishableArrayReadOnly = new ReadOnlyCollection<IWindow>(mPerishableArray);
            // Currently make the FRB XNA default to not using the UI, but the FRB MDX to true
            TextHeight = 2;
            TextSpacing = 1;

            mUIEnabled = true;



            //		sr.WriteLine("Inside the GuiManager constructor");
            //		sr.Close();
            mCursors = new List<Cursor>();

            mCursors.Add(cursor);

            mWindowArray = new List<IWindow>();
            mReadOnlyWindowArray = new ReadOnlyCollection<IWindow>(mWindowArray);

            mDominantWindows = new List<IWindow>();

#if !MONOGAME && !UNIT_TESTS && !XNA4
            RenderingBasedInitializize();
#endif

            BringsClickedWindowsToFront = true;


            nfi = new System.Globalization.NumberFormatInfo();
            //replaced the above line with the one below to used streamed images.


            ShowingCursorTextBox = true;

            renderingNotes = new List<String>();


            

            // Let's do some updates because we want to make sure our "last" values are set to the current value
            // so we don't have any movement on the cursor initially:
            cursor.Update(TimeManager.CurrentTime);

        }

        #endregion


        #region Public Methods

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

        public static void AddNextClickAction(Action action)
        {
#if DEBUG
            if(action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
#endif
            nextClickActions.Add(action);
        }

        public static void AddNextPushAction(Action action)
        {
#if DEBUG
            if(action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
#endif
            nextPushActions.Add(action);            
        }
        

        /// <summary>
        /// Adds a window as a Dominant Window.  If the window is a regular Window
        /// already managed by the GuiManager it will be removed from the regularly-managed
        /// window list. If the window is already a dominant window, this operation does nothing, so 
        /// it can be called multiple times.
        /// </summary>
        /// <param name="window">The window to add to the Dominant Window stack.</param>
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

#if !MONODROID
        static public Cursor GetCursorNum(int index)
        {
            if (index > -1 && index < mCursors.Count)
                return mCursors[index];
            else
                return null;
        }
#endif


        [Obsolete("Use AddDominantWindow instead, even if the window has already been added. This method will go away soon")]
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
        /// are drawn, which is what the user will usually expect.
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

        public static new string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("Number of regular Windows ").Append(mWindowArray.Count);

            return stringBuilder.ToString();
        }

        static public void UpdateDependencies()
        {
            foreach (IWindow w in mDominantWindows)
                w.UpdateDependencies();
            for (int i = 0; i < mWindowArray.Count; i++)
                mWindowArray[i].UpdateDependencies();
            for (int i = 0; i < mPerishableArray.Count; i++)
                mPerishableArray[i].UpdateDependencies();


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



            foreach (Cursor c in mCursors)
            {
                // Do this before the activity so on a click the frame with the click will still have a valid
                // WindowPushed.
                if ((c.PrimaryPush && c.WindowOver == null) || c.PrimaryClick)
                {
                    if (c.WindowPushed != null)
                    {
                        c.WindowPushed = null;
                    }
                }

                c.Update(TimeManager.CurrentTime);
                c.WindowOver = null;
                c.WindowClosing = null;



            }


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

                        if (window.Visible && !window.IgnoredByCursor && window.HasCursorOver(c))
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

                    void DoActivityOnWindowArray(List<IWindow> windowArray)
                    {
                        for (int i = windowArray.Count - 1; i > -1; i--)
                        {
                            // Code in this loop can call
                            // events.  Events may destroy
                            // entire groups of Windows, so we
                            // need to make sure we're still vaild:
                            if (i < windowArray.Count)
                            {
                                var window = windowArray[i];

                                // December 3, 2017
                                // The GuiManager used
                                // to continue the loop
                                // if UI was disabled, but
                                // that means that UI could
                                // be clicked-through. We don't
                                // want that, so we only continue
                                // if the UI is invisible. It's now
                                // the job of the IWindow implementation
                                // to check its own Enabled to figure out
                                // if it should process events or not...
                                //if (window.Visible == false || !window.Enabled)
                                if (window.Visible && !window.IgnoredByCursor && window.HasCursorOver(c))
                                {
                                    window.TestCollision(c);

                                    // HasCursorOver simply says "Is the cursor over this object".
                                    // You could be over an object but the object may not be enabled UI,
                                    // so we should only break the loop if WindowOver was actually assigned:
                                    if(c.WindowOver != null)
                                    {
                                        c.LastWindowOver = c.WindowOver;
                                        if (Cursor.PrimaryPush && i < mWindowArray.Count && BringsClickedWindowsToFront == true)
                                        {// we pushed a button, so let's bring it to the front
                                            windowArray.Remove(window); windowArray.Add(window);
                                        }
                                        break;
                                    }

                                }
                            }
                        }
                    }

                    var hasVisibleDominantWindows = DominantWindowActive;

                    #region if we have a dominant window
                    if (hasVisibleDominantWindows && c.WindowOver == null)
                    {
                        DoActivityOnWindowArray(mDominantWindows);
                    }
                    #endregion

                    #region looping through all regular windows

                    if (c.WindowOver == null && !hasVisibleDominantWindows)
                    {
                        DoActivityOnWindowArray(mWindowArray);

                    }
                    #endregion


                    // the click/push actions need to be after the UI activity
                    if (c.PrimaryClick)
                    {
                        if (nextClickActions.Count > 0)
                        {
                            var items = nextClickActions.ToList();
                            nextClickActions.Clear();
                            foreach (var item in items)
                            {
                                item();
                            }

                        }
                    }

                    if (c.PrimaryPush)
                    {
                        if (nextPushActions.Count > 0)
                        {
                            var items = nextPushActions.ToList();
                            nextPushActions.Clear();
                            foreach (var item in items)
                            {
                                item();
                            }
                        }
                    }

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

            #region letting go of any grabbed window
            if (Cursor.PrimaryClick)
            {
                Cursor.WindowGrabbed = null;
                Cursor.mSidesGrabbed = Sides.None;

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
	}
}
