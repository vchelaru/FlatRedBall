#region Using

using System;
using System.Collections.Generic;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
#if !SILVERLIGHT
#endif

#endregion

namespace LevelEditor.Screens
{
    public static class ScreenManager
    {
        #region Fields

        private static Screen mCurrentScreen;

        private static bool mWarnIfNotEmptyBetweenScreens = true;

        private static int mNumberOfFramesSinceLastScreenLoad = 0;

        private static Layer mNextScreenLayer;

        // The ScreenManager can be told to ignore certain objects which
        // we recognize will persist from screen to screen.  This should
        // NOT be used as a solution to get around the ScreenManager's check.
        private static PositionedObjectList<Camera> mPersistentCameras =
            new PositionedObjectList<Camera>();

        private static PositionedObjectList<SpriteFrame> mPersistentSpriteFrames =
            new PositionedObjectList<SpriteFrame>();

        private static PositionedObjectList<Text> mPersistentTexts =
            new PositionedObjectList<Text>();

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
        #endregion

        #region Methods

        #region Public Methods

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

            if (mCurrentScreen.IsActivityFinished)
            {
                string type = mCurrentScreen.NextScreen;
                mCurrentScreen.Destroy();

                // check to see if there is any leftover data
                CheckAndWarnIfNotEmpty();

                // Let's perform a GC here.  
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Loads the Screen, suspends input for one frame, and
                // calls Activity on the Screen.
                // The Activity call is required for objects like SpriteGrids
                // which need to be managed internally.
                mCurrentScreen = LoadScreen(type, false);

                mNumberOfFramesSinceLastScreenLoad = 0;
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


        public static ScreenType LoadScreen<ScreenType>(Layer layerToLoadScreenOn) where ScreenType : Screen, new()
        {
            mNextScreenLayer = layerToLoadScreenOn;

            ScreenType newScreen = new ScreenType();

            FlatRedBall.Input.InputManager.CurrentFrameInputSuspended = true;

            newScreen.Initialize();

            newScreen.Activity(true);

            return newScreen;
        }


        public static Screen LoadScreen(string screen, Layer layerToLoadScreenOn)
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

                newScreen.Initialize();

                newScreen.Activity(true);
            }

            return newScreen;
        }


        public static void Start<ScreenType>() where ScreenType : Screen, new()
        {
            mCurrentScreen = LoadScreen<ScreenType>(null);
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
                mCurrentScreen = LoadScreen(screenToStartWith, false);
            }
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

                // Managed Shapes
                if (ShapeManager.AutomaticallyUpdatedShapes.Count != 0)
                    messages.Add("There are " + ShapeManager.AutomaticallyUpdatedShapes.Count +
                        " Automatically Updated Shapes in the ShapeManager.");

                // Visible Circles
                if (ShapeManager.VisibleCircles.Count != 0)
                    messages.Add("There are " + ShapeManager.VisibleCircles.Count +
                        " visible Circles in the ShapeManager.");

                if (ShapeManager.VisibleRectangles.Count != 0)
                    messages.Add("There are " + ShapeManager.VisibleRectangles.Count +
                        " visible AxisAlignedRectangles in the VisibleRectangles.");

                if (ShapeManager.VisiblePolygons.Count != 0)
                    messages.Add("There are " + ShapeManager.VisiblePolygons.Count +
                        " visible Polygons in the ShapeManager.");

                if (ShapeManager.VisibleLines.Count != 0)
                    messages.Add("There are " + ShapeManager.VisibleLines.Count +
                        " visible Lines in the ShapeManager.");


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

