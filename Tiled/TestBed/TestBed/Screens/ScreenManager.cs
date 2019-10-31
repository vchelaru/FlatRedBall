#region Using
using System;
using System.Collections.Generic;

using FlatRedBall;
using FlatRedBall.Graphics;

#if !SILVERLIGHT
using FlatRedBall.Graphics.Model;
#endif

using FlatRedBall.ManagedSpriteGroups;

using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;

using FlatRedBall.Gui;
using FlatRedBall.Utilities;
using FlatRedBall.IO;

#if WINDOWS_PHONE
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;
#endif

#endregion

namespace TestBed.Screens
{

    public static partial class ScreenManager
    {
        #region Fields

        private static Screen mCurrentScreen;

        private static bool mSuppressStatePush = false;

#if !MONODROID
        private static BackStack<StackItem?> mBackStack = new BackStack<StackItem?>();
#endif

        private static bool mWarnIfNotEmptyBetweenScreens = true;

        private static int mNumberOfFramesSinceLastScreenLoad = 0;

        private static Layer mNextScreenLayer;

        // The ScreenManager can be told to ignore certain objects which
        // we recognize will persist from screen to screen.  This should
        // NOT be used as a solution to get around the ScreenManager's check.
        private static PositionedObjectList<Camera> mPersistentCameras = new PositionedObjectList<Camera>();

        private static PositionedObjectList<SpriteFrame> mPersistentSpriteFrames = new PositionedObjectList<SpriteFrame>();

        private static PositionedObjectList<Text> mPersistentTexts = new PositionedObjectList<Text>();

        #endregion

        #region Properties

        public static Screen CurrentScreen
        {
            get { return mCurrentScreen; }
        }

        public static Layer NextScreenLayer
        {
            get { return mNextScreenLayer; }
        }

        public static PositionedObjectList<Camera> PersistentCameras
        {
            get { return mPersistentCameras; }
        }

        public static PositionedObjectList<SpriteFrame> PersistentSpriteFrames
        {
            get { return mPersistentSpriteFrames; }
        }

        public static PositionedObjectList<Text> PersistentTexts
        {
            get { return mPersistentTexts; }
        }

        public static bool WarnIfNotEmptyBetweenScreens
        {
            get { return mWarnIfNotEmptyBetweenScreens; }
            set { mWarnIfNotEmptyBetweenScreens = value; }
        }
		
		public static bool ShouldActivateScreen
        {
            get;
            set;
        }
		
		public static Action<string> RehydrateAction
		{
			get;
			set;
		}
		
        #endregion

        #region Methods

        #region Public Methods

#if !MONODROID
        public static void PushStateToStack(int state)
        {
            if (!mSuppressStatePush && PlatformServices.BackStackEnabled)
            {
                mBackStack.MoveTo(new StackItem { State = state });
            }
        }

        public static void NavigateBack()
        {
            if (!PlatformServices.BackStackEnabled)
            {
                return;
            }

            StackItem? stackResult = null;

            // screens are added when moving to a new screen, while states are added as you move
            if (mBackStack.Current.HasValue && !string.IsNullOrEmpty(mBackStack.Current.Value.Screen))
            {
                stackResult = mBackStack.Current.Value;
                mBackStack.Back();
            }
            else
            {
                stackResult = mBackStack.Back();
            }

            if (!stackResult.HasValue)
            {
                // if we've gone to the beginning of the back stack, we should exit the game
                FlatRedBallServices.Game.Exit();
                return;
            }

            StackItem stackItem = stackResult.Value;
            if (!string.IsNullOrEmpty(stackItem.Screen))
            {
                mCurrentScreen.NextScreen = stackItem.Screen;
                mCurrentScreen.IsActivityFinished = true;
                mCurrentScreen.IsMovingBack = true;
            }
            else if (stackItem.State > -1)
            {
                // states are set as they are moved to, so if this is the last state, we should exit
                if (mBackStack.Count == 0)
                {
                    FlatRedBallServices.Game.Exit();
                    return;
                }

                mCurrentScreen.MoveToState(stackItem.State);
            }
        }
#endif

        #region XML Docs
        /// <summary>
        /// Calls activity on the current screen and checks to see if screen
        /// activity is finished.  If activity is finished, the current Screen's
        /// NextScreen is loaded.
        /// </summary>
        #endregion
        public static void Activity()
        {
            if (mCurrentScreen == null) return;

            mCurrentScreen.Activity(false);

            mCurrentScreen.ActivityCallCount++;

            if (mCurrentScreen.IsActivityFinished)
            {
                GuiManager.Cursor.IgnoreNextClick = true;
                string type = mCurrentScreen.NextScreen;
                Screen asyncLoadedScreen = mCurrentScreen.mNextScreenToLoadAsync;

#if !MONODROID
                if (!mCurrentScreen.IsMovingBack && PlatformServices.BackStackEnabled)
                {
                    StackItem item = new StackItem { Screen = mCurrentScreen.GetType().FullName, State = -1 };
                    mBackStack.MoveTo(item, mCurrentScreen.BackStackBehavior);
                }
#endif

                mCurrentScreen.Destroy();

                // check to see if there is any leftover data
                CheckAndWarnIfNotEmpty();

                // Let's perform a GC here.  
                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (asyncLoadedScreen == null)
                {

                    // Loads the Screen, suspends input for one frame, and
                    // calls Activity on the Screen.
                    // The Activity call is required for objects like SpriteGrids
                    // which need to be managed internally.

                    // No need to assign mCurrentScreen - this is done by the 4th argument "true"
                    //mCurrentScreen = 
                    LoadScreen(type, null, true, true);

                    mNumberOfFramesSinceLastScreenLoad = 0;

                }
                else
                {

                    mCurrentScreen = asyncLoadedScreen;

                    mCurrentScreen.AddToManagers();

                    mCurrentScreen.Activity(true);


                    mCurrentScreen.ActivityCallCount++;
                    mNumberOfFramesSinceLastScreenLoad = 0;
                }
            }
            else
            {
                mNumberOfFramesSinceLastScreenLoad++;
            }
        }


        public static Screen LoadScreen(string screen, bool createNewLayer)
        {
            if (createNewLayer)
            {
                return LoadScreen(screen, SpriteManager.AddLayer());
            }
            else
            {
                return LoadScreen(screen, (Layer)null);
            }
        }


        public static T LoadScreen<T>(Layer layerToLoadScreenOn) where T : Screen
        {
            mNextScreenLayer = layerToLoadScreenOn;

#if XBOX360
            T newScreen = (T)Activator.CreateInstance(typeof(T));
#else
            T newScreen = (T)Activator.CreateInstance(typeof(T), new object[0]);
#endif

            FlatRedBall.Input.InputManager.CurrentFrameInputSuspended = true;

            newScreen.Initialize(true);

#if !MONODROID
            if (mBackStack.Current.HasValue && mBackStack.Current.Value.State > -1 && PlatformServices.BackStackEnabled)
            {
                newScreen.MoveToState(mBackStack.Current.Value.State);
            }
#endif

            newScreen.Activity(true);

            newScreen.ActivityCallCount++;

            return newScreen;
        }


        public static Screen LoadScreen(string screen, Layer layerToLoadScreenOn)
        {
            return LoadScreen(screen, layerToLoadScreenOn, true, false);
        }

        public static Screen LoadScreen(string screen, Layer layerToLoadScreenOn, bool addToManagers, bool makeCurrentScreen)
        {
            mNextScreenLayer = layerToLoadScreenOn;

            Screen newScreen = null;

            Type typeOfScreen = Type.GetType(screen);

            if (typeOfScreen == null)
            {
                throw new System.ArgumentException("There is no " + screen + " class defined in your project or linked assemblies.");
            }

            if (screen != null && screen != "")
            {
#if XBOX360
                newScreen = (Screen)Activator.CreateInstance(typeOfScreen);
#else
                newScreen = (Screen)Activator.CreateInstance(typeOfScreen, new object[0]);
#endif
            }

            if (newScreen != null)
            {
                FlatRedBall.Input.InputManager.CurrentFrameInputSuspended = true;

#if !MONODROID
                mSuppressStatePush = mBackStack.Current.HasValue && mBackStack.Current.Value.State > -1;
#endif

                newScreen.Initialize(addToManagers);

#if !MONODROID
                if (mSuppressStatePush)
                {
                    newScreen.MoveToState(mBackStack.Current.Value.State);
                }
#endif
                mSuppressStatePush = false;

                if (addToManagers)
                {
					// We do this so that new Screens are the CurrentScreen in Activity.
					// This is useful in custom logic.
				    if (makeCurrentScreen)
                    {
                        mCurrentScreen = newScreen;
                    }
					
                    newScreen.Activity(true);


                    newScreen.ActivityCallCount++;
                }
            }

            return newScreen;
        }

        public static void Start<T>() where T : Screen, new()
        {
            mCurrentScreen = LoadScreen<T>(null);
        }

        #region XML Docs
        /// <summary>
        /// Loads a screen.  Should only be called once during initialization.
        /// </summary>
        /// <param name="screenToStartWith">Qualified name of the class to load.</param>
        #endregion
        public static void Start(string screenToStartWith)
        {
            if (mCurrentScreen != null)
            {
                throw new InvalidOperationException("You can't call Start if there is already a Screen.  Did you call Start twice?");
            }
            else
            {
                StateManager.Current.Activating += new Action(OnStateActivating);
                StateManager.Current.Deactivating += new Action(OnStateDeactivating);
                StateManager.Current.Initialize();

#if !MONODROID
                //if the state manager overwrote the backstack from tombstone (WP7), it will have a different current, 
                //otherwise, it will be the same value as screenToStartWith.
                if (mBackStack.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("resuming with backstack containing " +
                        mBackStack.Count + " items with current being " + mBackStack.Current.Value.Screen);
                    screenToStartWith = mBackStack.Current.Value.Screen;
                    mBackStack.Back();
                }
#endif


                if (ShouldActivateScreen && RehydrateAction != null)
                {
					RehydrateAction(screenToStartWith);
                }
                else
                {
                    mCurrentScreen = LoadScreen(screenToStartWith, null, true, true);

                    ShouldActivateScreen = false;
                }
            }
        }

        /// <summary>Do all state deactivation work here.</summary>
        private static void OnStateDeactivating()
        {
#if !MONODROID
            mBackStack.MoveTo(new StackItem { Screen = mCurrentScreen.GetType().FullName, State = -1 });
            StateManager.Current["backstack"] = mBackStack;
#endif
        }

        /// <summary>Do all state activation work here</summary>
        private static void OnStateActivating()
        {
#if !MONODROID
            mBackStack = StateManager.Current.Get<BackStack<StackItem?>>("backstack");
        
			ShouldActivateScreen = true;
#endif
		}


        public static new string ToString()
        {
            if (mCurrentScreen != null)
                return mCurrentScreen.ToString();
            else
                return "No Current Screen";
        }

        #endregion

        #region Private Methods
        public static void CheckAndWarnIfNotEmpty()
        {
            if (WarnIfNotEmptyBetweenScreens)
            {
                List<string> messages = new List<string>();
                // the user wants to make sure that the Screens have cleaned up everything
                // after being destroyed.  Check the data to make sure it's all empty.

                #region Make sure there's only 1 non-persistent Camera left
                if (SpriteManager.Cameras.Count > 1)
                {
                    int count = SpriteManager.Cameras.Count;

                    foreach (Camera camera in mPersistentCameras)
                    {
                        if (SpriteManager.Cameras.Contains(camera))
                        {
                            count--;
                        }
                    }

                    if (count > 1)
                    {
                        messages.Add("There are " + count +
                            " Cameras in the SpriteManager (excluding ignored Cameras).  There should only be 1.");
                    }
                }
                #endregion

                #region Make sure that the Camera doesn't have any extra layers

                if (SpriteManager.Camera.Layers.Count > 1)
                {
                    messages.Add("There are " + SpriteManager.Camera.Layers.Count + " Layers on the default Camera.  There should only be 1");
                }

                #endregion

                #region Automatically updated Sprites
                if (SpriteManager.AutomaticallyUpdatedSprites.Count != 0)
                {
                    int spriteCount = SpriteManager.AutomaticallyUpdatedSprites.Count;

                    foreach (SpriteFrame spriteFrame in mPersistentSpriteFrames)
                    {
                        foreach (Sprite sprite in SpriteManager.AutomaticallyUpdatedSprites)
                        {
                            if (spriteFrame.IsSpriteComponentOfThis(sprite))
                            {
                                spriteCount--;
                            }
                        }
                    }

                    if (spriteCount != 0)
                    {
                        messages.Add("There are " + spriteCount +
                            " AutomaticallyUpdatedSprites in the SpriteManager.");
                    }

                }
                #endregion

                #region Manually updated Sprites
                if (SpriteManager.ManuallyUpdatedSpriteCount != 0)
                    messages.Add("There are " + SpriteManager.ManuallyUpdatedSpriteCount +
                        " ManuallyUpdatedSprites in the SpriteManager.");
                #endregion

                #region Ordered by distance Sprites

                if (SpriteManager.OrderedSprites.Count != 0)
                {
                    int spriteCount = SpriteManager.OrderedSprites.Count;

                    foreach (SpriteFrame spriteFrame in mPersistentSpriteFrames)
                    {
                        foreach (Sprite sprite in SpriteManager.OrderedSprites)
                        {
                            if (spriteFrame.IsSpriteComponentOfThis(sprite))
                            {
                                spriteCount--;
                            }
                        }
                    }

                    if (spriteCount != 0)
                    {
                        messages.Add("There are " + spriteCount +
                            " Ordered (Drawn) Sprites in the SpriteManager.");
                    }

                }

                #endregion

                #region Managed Positionedobjects
                if (SpriteManager.ManagedPositionedObjects.Count != 0)
                    messages.Add("There are " + SpriteManager.ManagedPositionedObjects.Count +
                        " Managed PositionedObjects in the SpriteManager.");

                #endregion

                #region Layers
                if (SpriteManager.LayerCount != 0)
                    messages.Add("There are " + SpriteManager.LayerCount +
                        " Layers in the SpriteManager.");

                #endregion

                #region TopLayer

                if (SpriteManager.TopLayer.Sprites.Count != 0)
                {
                    messages.Add("There are " + SpriteManager.TopLayer.Sprites.Count +
                        " Sprites in the SpriteManager's TopLayer.");
                }

                #endregion

                #region Particles
                if (SpriteManager.ParticleCount != 0)
                    messages.Add("There are " + SpriteManager.ParticleCount +
                        " Particle Sprites in the SpriteManager.");

                #endregion

                #region SpriteFrames
                if (SpriteManager.SpriteFrames.Count != 0)
                {
                    int spriteFrameCount = SpriteManager.SpriteFrames.Count;

                    foreach (SpriteFrame spriteFrame in mPersistentSpriteFrames)
                    {
                        if (SpriteManager.SpriteFrames.Contains(spriteFrame))
                        {
                            spriteFrameCount--;
                        }
                    }

                    if (spriteFrameCount != 0)
                    {
                        messages.Add("There are " + spriteFrameCount +
                            " SpriteFrames in the SpriteManager.");
                    }

                }
                #endregion

                #region Text objects
                if (TextManager.AutomaticallyUpdatedTexts.Count != 0)
                {
                    int textCount = TextManager.AutomaticallyUpdatedTexts.Count;

                    foreach (Text text in mPersistentTexts)
                    {
                        if (TextManager.AutomaticallyUpdatedTexts.Contains(text))
                        {
                            textCount--;
                        }
                    }

                    if (textCount != 0)
                    {
                        messages.Add("There are " + textCount +
                            "automatically updated Texts in the TextManager.");
                    }
                }
                #endregion

                #region Managed Shapes
                if (ShapeManager.AutomaticallyUpdatedShapes.Count != 0)
                    messages.Add("There are " + ShapeManager.AutomaticallyUpdatedShapes.Count +
                        " Automatically Updated Shapes in the ShapeManager.");
                #endregion

                #region  Visible Circles
                if (ShapeManager.VisibleCircles.Count != 0)
                    messages.Add("There are " + ShapeManager.VisibleCircles.Count +
                        " visible Circles in the ShapeManager.");
                #endregion

                #region Visible Rectangles

                if (ShapeManager.VisibleRectangles.Count != 0)
                    messages.Add("There are " + ShapeManager.VisibleRectangles.Count +
                        " visible AxisAlignedRectangles in the VisibleRectangles.");

                #endregion

                #region Visible Polygons

                if (ShapeManager.VisiblePolygons.Count != 0)
                    messages.Add("There are " + ShapeManager.VisiblePolygons.Count +
                        " visible Polygons in the ShapeManager.");
                #endregion

                #region Visible Lines

                if (ShapeManager.VisibleLines.Count != 0)
                    messages.Add("There are " + ShapeManager.VisibleLines.Count +
                        " visible Lines in the ShapeManager.");
                #endregion

                #region Automatically Updated Positioned Models
#if !SILVERLIGHT && !MONODROID
                if (ModelManager.AutomaticallyUpdatedModels.Count != 0)
                {
                    messages.Add("There are " + ModelManager.AutomaticallyUpdatedModels.Count +
                        " managed PositionedModels in the ModelManager.");
                }
#endif
                #endregion

                if (messages.Count != 0)
                {
                    string errorString = "The Screen that was just unloaded did not clean up after itself:";
                    foreach (string s in messages)
                        errorString += "\n" + s;

                    throw new System.Exception(errorString);
                }
            }
        }
        #endregion

        #endregion
    }
}

