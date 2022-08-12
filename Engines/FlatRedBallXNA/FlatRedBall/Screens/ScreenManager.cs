#region Using

using System;
using System.Collections.Generic;
using System.Reflection;
using FlatRedBall.Graphics;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Gui;
using FlatRedBall.Utilities;
using FlatRedBall.IO;
using System.Linq;
using Microsoft.Xna.Framework.Content;
#if !SILVERLIGHT
#endif

#if WINDOWS_PHONE
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;
#endif

#endregion

namespace FlatRedBall.Screens
{

    public static partial class ScreenManager
    {
        #region Fields

        static bool? mWasFixedTimeStep = null;
        static double? mLastTimeFactor = null;

        private static Screen mCurrentScreen;

        private static bool mSuppressStatePush = false;

        private static bool mWarnIfNotEmptyBetweenScreens = true;

        private static int mNumberOfFramesSinceLastScreenLoad = 0;

        private static Layer mNextScreenLayer;

        // The ScreenManager can be told to ignore certain objects which
        // we recognize will persist from screen to screen.  This should
        // NOT be used as a solution to get around the ScreenManager's check.
        private static PositionedObjectList<Camera> mPersistentCameras = new PositionedObjectList<Camera>();

        private static PositionedObjectList<SpriteFrame> mPersistentSpriteFrames = new PositionedObjectList<SpriteFrame>();

        private static PositionedObjectList<Text> mPersistentTexts = new PositionedObjectList<Text>();

        private static List<IDrawableBatch> mPersistentDrawableBatches = new List<IDrawableBatch>();

        private static Action<Screen> nextCallback;

        public static bool IsInEditMode { get; private set; }
        public static bool IsNextScreenInEditMode { get; set; }

        #endregion

        #region Properties

        public static Assembly MainAssembly { get; private set; }

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

        public static PositionedObjectList<Sprite> PersistentSprites { get; private set; } = new PositionedObjectList<Sprite>();
        public static PositionedObjectList<PositionedObject> PersistentPositionedObjects { get; private set; } = new PositionedObjectList<PositionedObject>();

        /// <summary>
        /// A list of IDrawableBatch instances which persist inbetween screens. Items in this list
        /// do not result in exceptions if they are not cleaned up inbetween screens.
        /// </summary>
        public static List<IDrawableBatch> PersistentDrawableBatches
        {
            get
            {
                return mPersistentDrawableBatches;
            }
        }

        public static PositionedObjectList<Text> PersistentTexts
        {
            get { return mPersistentTexts; }
        }

        public static List<IWindow> PersistentWindows
        {
            get; private set;
        } = new List<IWindow>();

        public static PositionedObjectList<AxisAlignedRectangle> PersistentAxisAlignedRectangles
        {
            get; private set;
        } = new PositionedObjectList<AxisAlignedRectangle>();

        public static PositionedObjectList<Polygon> PersistentPolygons
        {
            get; private set;
        } = new PositionedObjectList<Polygon>();

        public static PositionedObjectList<Line> PersistentLines
        {
            get; private set;
        } = new PositionedObjectList<Line>();

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
        #endregion

        static Exception GlueViewLoadException;
		
		public static Action<string> RehydrateAction
		{
			get;
			set;
		}
		
        /// <summary>
        /// Event raised before a screen's CustomInitialize is called, allowing systems (like the level editor) to
        /// run code before the user's custom code.
        /// </summary>
        public static event Action<Screen> BeforeScreenCustomInitialize;
        public static event Action<Screen> ScreenLoaded;

        /// <summary>
        /// Event raised after a screen's Destroy is called.
        /// </summary>
        /// <remarks>
        /// This is called before checking for leftover objects (such as Entities which haven't been destroyed).
        /// </remarks>
        public static event Action<Screen> AfterScreenDestroyed;

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
            /////////////////Early Out///////////////////////////
            if (mCurrentScreen == null) return;
            //////////////End Early Out//////////////////////////

            if(!IsInEditMode)
            {
                mCurrentScreen.Activity(mCurrentScreen.ActivityCallCount == 0);

                mCurrentScreen.ActivityCallCount++;
            }
            else
            {
                if(!mCurrentScreen.IsActivityFinished)
                {
                    mCurrentScreen.ActivityEditMode();
                }
                if(GlueViewLoadException != null)
                {
                    FlatRedBall.Debugging.Debugger.Write(GlueViewLoadException.ToString());
                }
            }

            if (mCurrentScreen.ActivityCallCount == 1 && mWasFixedTimeStep.HasValue)
            {
                FlatRedBallServices.Game.IsFixedTimeStep = mWasFixedTimeStep.Value;
                TimeManager.TimeFactor = mLastTimeFactor.Value;
            }

            if (mCurrentScreen.IsActivityFinished)
            {
                string type = mCurrentScreen.NextScreen;

#if DEBUG
                if(string.IsNullOrWhiteSpace(type))
                {
                    var message = $"The current screen ({mCurrentScreen.GetType()}) just ended but didn't specify a NextScreen. " +
                        $"You can specify the next screen by calling MoveToScreen or by manually setting the NextScreen";
                }
#endif
                var isFullyQualified = type.Contains(".");
                if(!isFullyQualified)
                {
                    // try to prepend the current type to make the next screen fully qualified:
                    var prepend = mCurrentScreen.GetType().Namespace;
                    type = prepend + "." + type;
                }

                Screen asyncLoadedScreen = mCurrentScreen.mNextScreenToLoadAsync;



                // October 10, 2020
                // We used to set the time and values before calling
                // destroy. Now we want to call Destroy on the screen first
                // in case Destroy happens to change the time factor:
                mCurrentScreen.Destroy();

                AfterScreenDestroyed?.Invoke(mCurrentScreen);

                mWasFixedTimeStep = FlatRedBallServices.Game.IsFixedTimeStep;
                mLastTimeFactor = TimeManager.TimeFactor;
                FlatRedBallServices.Game.IsFixedTimeStep = false;
                TimeManager.TimeFactor = 0;
                GuiManager.Cursor.IgnoreInputThisFrame = true;
                if(Input.InputManager.InputReceiver != null)
                {
                    Input.InputManager.InputReceiver = null;
                }
                Instructions.InstructionManager.ObjectsIgnoringPausing.Clear();
                // Added Nov 15 2020 - do we want this here? If not we may get
                // silent accumulation. Do we warn or just destroy?
                Instructions.InstructionManager.Instructions.Clear();

                FlatRedBallServices.singleThreadSynchronizationContext.Clear();

                //mCurrentScreen.Destroy();

                // check to see if there is any leftover data
                CheckAndWarnIfNotEmpty(mCurrentScreen);

                // Let's perform a GC here.  
                GC.Collect();

				// Not sure why this started to freeze on Android in the automated test project
				// on April 22, 2015. I'm commenting it out because I don't think we need to wait
				// for finalizers, and we can just continue on. Maybe try to bring the code back
				// on Android in the future too.
                // March 16, 2017 - Desktop GL too, not sure why...
				#if !ANDROID && !DESKTOP_GL
                GC.WaitForPendingFinalizers();
				#endif

                if (asyncLoadedScreen == null)
                {

                    // Loads the Screen, suspends input for one frame, and
                    // calls Activity on the Screen.
                    // The Activity call is required for objects like SpriteGrids
                    // which need to be managed internally.

                    // No need to assign mCurrentScreen - this is done by the 4th argument "true"
                    //mCurrentScreen = 
                    LoadScreen(type);
                }
                else
                {

                    mCurrentScreen = asyncLoadedScreen;

                    nextCallback?.Invoke(mCurrentScreen);
                    nextCallback = null;

                    mCurrentScreen.AddToManagers();


                }
                mNumberOfFramesSinceLastScreenLoad = 0;
            }
            else
            {
                mNumberOfFramesSinceLastScreenLoad++;
            }
        }


        public static void Start<T>() where T : Screen, new()
        {
            var type = typeof(T);
            Start(type);
        }

        /// <summary>
        /// Ends the current screen and moves to the next screen.
        /// </summary>
        /// <param name="screenType">The screen to move to.</param>
        /// <param name="screenCreatedCallback">An event to call after the screen has been created.</param>
        /// <remarks>
        /// This method provides an alternative to the screen managing its own flow through its MoveMoveToScreen method.
        /// This method can be used by objects outside of screens managing flow.
        /// </remarks>
        public static void MoveToScreen(Type screenType, Action<Screen> screenCreatedCallback = null)
        {
            if(mCurrentScreen != null)
            {
                mCurrentScreen.MoveToScreen(screenType);
                nextCallback = screenCreatedCallback;
            }
            else
            {
                throw new Exception("There is no current screen to move from. Call Start to create the first screen.");
            }
        }

        public static void MoveToScreen(string screenType, Action<Screen> screenCreatedCallback = null)
        {
            if (mCurrentScreen != null)
            {
                mCurrentScreen.MoveToScreen(screenType);
                nextCallback = screenCreatedCallback;
            }
            else
            {
                throw new Exception("There is no current screen to move from. Call Start to create the first screen.");
            }
        }

        /// <summary>
        /// Loads a screen.  Should only be called once during initialization.
        /// </summary>
        /// <param name="screenToStartWithType">Qualified name of the class to load.</param>
        public static void Start(Type screenToStartWithType)
        {

#if WINDOWS_8 || UWP
            MainAssembly =
                screenToStartWithType.GetTypeInfo().Assembly;
#else
            MainAssembly =
                screenToStartWithType.Assembly;
#endif
            string screenToStartWith = screenToStartWithType.FullName;

            if (mCurrentScreen != null)
            {
                throw new InvalidOperationException("You can't call Start if there is already a Screen.  Did you call Start twice?");
            }
            else
            {
                StateManager.Current.Initialize();

                if (ShouldActivateScreen && RehydrateAction != null)
                {
					RehydrateAction(screenToStartWith);
                }
                else
                {
                    mCurrentScreen = LoadScreen(screenToStartWith);

                    ShouldActivateScreen = false;
                }

                
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

        #region Internal Methods

        internal static void Draw()
        {
            if(mCurrentScreen != null)
            {
                mCurrentScreen.HasDrawBeenCalled = true;
            }

        }

        internal static void UpdateDependencies()
        {
            mCurrentScreen?.UpdateDependencies(TimeManager.CurrentTime);
        }

        #endregion

        #region Private Methods

        private static Screen LoadScreen(string screen)
        {
            mNextScreenLayer = null;

            Screen newScreen = null;

            Type typeOfScreen = MainAssembly.GetType(screen);

            if (typeOfScreen == null)
            {
                throw new System.ArgumentException("There is no " + screen + " class defined in your project or linked assemblies.");
            }

            IsInEditMode = IsNextScreenInEditMode;
            IsNextScreenInEditMode = false;

            if (screen != null && screen != "")
            {
                if(typeOfScreen.IsAbstract == false)
                {
                    newScreen = (Screen)Activator.CreateInstance(typeOfScreen, new object[0]);
                }
            }

            if (newScreen != null)
            {
                FlatRedBall.Input.InputManager.CurrentFrameInputSuspended = true;

                // We do this so that new Screens are the CurrentScreen in Activity.
                // This is useful in custom logic.
                mCurrentScreen = newScreen;

                if(BeforeScreenCustomInitialize != null)
                {
                    mCurrentScreen.BeforeCustomInitialize += () => BeforeScreenCustomInitialize(mCurrentScreen);
                }
                if(IsInEditMode)
                {
                    GlueViewLoadException = null;

                    try
                    {
                        // in edit mode, we tolerate crashes on initialize since this is common 
                        newScreen.Initialize(true);
                        TimeManager.SetNextFrameTimeTo0 = true;

                        newScreen.ApplyRestartVariables();


                        // stop everything:
                        foreach (var item in SpriteManager.ManagedPositionedObjects)
                        {
                            item.Velocity = Microsoft.Xna.Framework.Vector3.Zero;
                            item.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;
                        }
                        foreach(var shape in ShapeManager.AutomaticallyUpdatedShapes)
                        {
                            shape.Velocity = Microsoft.Xna.Framework.Vector3.Zero;
                            shape.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;
                        }
                    }
                    catch(Exception e)
                    {
                        // I guess do nothing?
                        GlueViewLoadException = e;
                    }

                }
                else
                {
                    newScreen.Initialize(true);
                    TimeManager.SetNextFrameTimeTo0 = true;

                    newScreen.ApplyRestartVariables();
                }

                mSuppressStatePush = false;

                nextCallback?.Invoke(newScreen);
                nextCallback = null;

                // Dec 28, 2020
                // I thought we called
                // Activity immediately
                // when a new Screen was
                // created/added. If we don't
                // then a single frame will pass
                // without activity, and objects may
                // not be positioned correclty.
                if (!IsInEditMode)
                {
                    mCurrentScreen.Activity(mCurrentScreen.ActivityCallCount == 0);

                    mCurrentScreen.ActivityCallCount++;
                }

                // We want to set time factor back to non-zero if in edit mode so objects can move and update
                if (IsInEditMode || ( mCurrentScreen.ActivityCallCount == 1 && mWasFixedTimeStep.HasValue))
                {
                    FlatRedBallServices.Game.IsFixedTimeStep = mWasFixedTimeStep.Value;
                    TimeManager.TimeFactor = mLastTimeFactor.Value;
                }

                ScreenLoaded?.Invoke(mCurrentScreen);

                if (IsInEditMode)
                {
                    // stop everything:
                    foreach (var item in SpriteManager.ManagedPositionedObjects)
                    {
                        // this prevents flickering due to the upate dependencies call
                        // having been run earlier in the frame.
                        item.ForceUpdateDependenciesDeep();
                    }
                }

            }

            return newScreen;
        }

        /// <summary>
        /// Checks if all FlatRedBall managers (such as the SpriteManager) have been emptied out in preparation for
        /// screen transition. If not, an informative exception is thrown indicating what is left over. Normally this is not
        /// called in custom code since it is automatically called by the ScreenManager when a Screen is transitioning.
        /// </summary>
        /// <param name="screen">The Screen in question.</param>
        /// <exception cref="System.Exception">Thrown if any manager is not empty.</exception>
        public static void CheckAndWarnIfNotEmpty(Screen screen = null)
        {

            if (WarnIfNotEmptyBetweenScreens)
            {
                List<string> messages = new List<string>();
                // the user wants to make sure that the Screens have cleaned up everything
                // after being destroyed.  Check the data to make sure it's all empty.

                // Currently we're not checking the GuiManager - do we want to?

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
                            " Cameras in the SpriteManager (excluding ignored Cameras).  There should only be 1.  See \"FlatRedBall.SpriteManager.Cameras\"");
                    }
                }
                #endregion

                #region Make sure that the Camera doesn't have any extra layers

                if (SpriteManager.Camera.Layers.Count > 1)
                {
                    messages.Add("There are " + SpriteManager.Camera.Layers.Count +
                        " Layers on the default Camera.  There should only be 1.  See \"FlatRedBall.SpriteManager.Camera.Layers\"");
                }

                #endregion

                #region Automatically updated Sprites
                if (SpriteManager.AutomaticallyUpdatedSprites.Count != 0)
                {
                    var remainingSprites = SpriteManager.AutomaticallyUpdatedSprites.ToList();
                    remainingSprites.RemoveAll(item => mPersistentSpriteFrames.Any(frame => frame.IsSpriteComponentOfThis(item)));
                    remainingSprites.RemoveAll(item => PersistentSprites.Contains(item));

                    int spriteCount = remainingSprites.Count;

                    if (spriteCount != 0)
                    {
                        var message = "There are " + spriteCount +
                            " AutomaticallyUpdatedSprites in the SpriteManager. See \"FlatRedBall.SpriteManager.AutomaticallyUpdatedSprites\"";

                        var first = remainingSprites.FirstOrDefault();

                        if(first != null)
                        {
                            message += $"\nFirst sprite Name:{first.Name ?? "<null>"}, Position:{first.Position}, Parent:{first.Parent?.Name ?? "<null>"}, Texture:{first.Texture?.Name ?? "<null>"}";
                        }

                        messages.Add(message);
                    }

                }
                #endregion

                #region Manually updated Sprites
                if (SpriteManager.ManuallyUpdatedSpriteCount != 0)
                {
                    messages.Add("There are " + SpriteManager.ManuallyUpdatedSpriteCount +
                        " ManuallyUpdatedSprites in the SpriteManager.  See \"SpriteManager.ManuallyUpdatedSpriteCount\"");

                    for(int i = 0; i < SpriteManager.ManuallyUpdatedSpriteCount; i++)
                    {
                        var sprite = SpriteManager.ManuallyUpdatedSprites[i];
                        if(!string.IsNullOrEmpty(sprite.Name) )
                        {
                            messages.Add($"Manually Updated Sprite: {sprite.Name}");
                        }
                    }
                }
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
                            " Ordered (Drawn) Sprites in the SpriteManager.  See \"FlatRedBall.SpriteManager.OrderedSprites\"");
                    }

                }

                #endregion

                #region Drawable Batches

                if (SpriteManager.DrawableBatches.Count != 0)
                {
                    int drawableBatchCount = 0;
                    IDrawableBatch firstDrawableBatch = null;
                    foreach(var item in SpriteManager.DrawableBatches)
                    {
                        if(!PersistentDrawableBatches.Contains(item))
                        {
                            drawableBatchCount++;
                            firstDrawableBatch = item;
                        }
                    }

                    if (drawableBatchCount > 0)
                    {
                        var message = "There are " + drawableBatchCount +
                            " DrawableBatches in the SpriteManager.  " +
                            "See  \"FlatRedBall.SpriteManager.DrawableBatches\"";

                        message += $"\nFirst IDB type: {firstDrawableBatch.GetType()}";

                        messages.Add(message);


                    }
                }

                #endregion

                #region Managed Positionedobjects
                if (SpriteManager.ManagedPositionedObjects.Count != 0)
                {
                    var count = SpriteManager.ManagedPositionedObjects.Count;

                    foreach(var persistentPositionedObject in PersistentPositionedObjects)
                    {
                        if(persistentPositionedObject.ListsBelongingTo.Contains(SpriteManager.mManagedPositionedObjects))
                        {
                            count--;
                        }
                    }

                    if(count > 1)
                    {
                        messages.Add("There are " + count +
                            " Managed PositionedObjects in the SpriteManager.  See \"FlatRedBall.SpriteManager.ManagedPositionedObjects\"");

                        var firstPositionedObject = SpriteManager.ManagedPositionedObjects.Except(PersistentPositionedObjects).FirstOrDefault();
                        var type = firstPositionedObject.GetType();

                        if (type.FullName.Contains(".Entities."))
                        {
                            string message;
                            if(string.IsNullOrWhiteSpace(firstPositionedObject.Name))
                            {
                                message = $"The first is an unnnamed entity of type {type.Name}";
                            }
                            else
                            {
                                message = $"The first is an entity of type {type.Name} named {firstPositionedObject.Name}";
                            }
                            messages.Add(message);
                        }

                    }
                }
                #endregion

                #region Layers
                if (SpriteManager.LayerCount != 0)
                {
                    
                    messages.Add("There are " + SpriteManager.LayerCount +
                        " Layers in the SpriteManager.  See \"FlatRedBall.SpriteManager.Layers\"");
                }
                #endregion

                #region TopLayer

                if (SpriteManager.TopLayer.Sprites.Count != 0)
                {
                    var count = SpriteManager.TopLayer.Sprites.Count;
                    foreach (var sprite in PersistentSprites)
                    {
                        if(sprite.ListsBelongingTo.Contains(SpriteManager.TopLayer.mSprites))
                        {
                            count--;
                        }
                    }

                    if (count > 0)
                    { 
                        messages.Add("There are " + count +
                            " Sprites in the SpriteManager's TopLayer.  See \"FlatRedBall.SpriteManager.TopLayer.Sprites\"");
                    }

                }

                #endregion

                #region Particles
                if (SpriteManager.ParticleCount != 0)
                {
                    messages.Add("There are " + SpriteManager.ParticleCount +
                        " Particle Sprites in the SpriteManager.  See \"FlatRedBall.SpriteManager.AutomaticallyUpdatedSprites\"");

                }
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
                            " SpriteFrames in the SpriteManager.  See \"FlatRedBall.SpriteManager.SpriteFrames\"");
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
                        var textError =
                            "There are " + textCount +
                            " automatically updated Texts in the TextManager.  See \"FlatRedBall.Graphics.TextManager.AutomaticallyUpdatedTexts\"";
                        messages.Add(textError);
                    }
                }
                #endregion

                #region Managed Shapes
                if (ShapeManager.AutomaticallyUpdatedShapes.Count != 0)
                {
                    var message = "There are " + ShapeManager.AutomaticallyUpdatedShapes.Count +
                        " Automatically Updated Shapes in the ShapeManager.  See \"FlatRedBall.Math.Geometry.ShapeManager.AutomaticallyUpdatedShapes\"";

                    var first = ShapeManager.AutomaticallyUpdatedShapes[0];
                    message += $"\nFirst shape: {first}";
                    
                    messages.Add(message);
                }
                #endregion

                #region  Visible Circles
                if (ShapeManager.VisibleCircles.Count != 0)
                {
                    messages.Add("There are " + ShapeManager.VisibleCircles.Count +
                        " visible Circles in the ShapeManager.  See \"FlatRedBall.Math.Geometry.ShapeManager.VisibleCircles\"");
                }
                #endregion

                #region Visible Rectangles

                if (ShapeManager.VisibleRectangles.Count != 0)
                {
                    var rectangleCount = ShapeManager.VisibleRectangles.Count;
                    foreach(var rectangle in PersistentAxisAlignedRectangles)
                    {
                        if(ShapeManager.VisibleRectangles.Contains(rectangle))
                        {
                            rectangleCount--;
                        }
                    }
                    if(rectangleCount != 0)
                    {
                        messages.Add($"There are {rectangleCount}" +
                            " visible AxisAlignedRectangles in the VisibleRectangles.  See \"FlatRedBall.Math.Geometry.ShapeManager.VisibleRectangles\"");

                    }
                }
                #endregion

                #region Visible Polygons

                if (ShapeManager.VisiblePolygons.Count != 0)
                {
                    var polygonCount = ShapeManager.VisiblePolygons.Count;
                    foreach(var polygon in PersistentPolygons)
                    {
                        if(ShapeManager.VisiblePolygons.Contains(polygon))
                        {
                            polygonCount--;
                        }
                    }
                    if(polygonCount != 0)
                    {
                        messages.Add("There are " + polygonCount +
                            " visible Polygons in the ShapeManager.  See \"FlatRedBall.Math.Geometry.ShapeManager.VisiblePolygons\"");

                    }
                }
                #endregion

                #region Visible Lines

                if (ShapeManager.VisibleLines.Count != 0)
                {
                    var lineListCopy = ShapeManager.VisibleLines
                        .Except(PersistentLines)
                        .ToList();

                    if(lineListCopy.Count > 0)
                    {
                        messages.Add("There are " + lineListCopy.Count +
                            " visible Lines in the ShapeManager.  See \"FlatRedBall.Math.Geometry.ShapeManager.VisibleLines\"");
                    }
                }
                #endregion

                #region IWindows

                if (GuiManager.Windows.Count != 0)
                {
                    var remainingWindows = GuiManager.Windows.Except(PersistentWindows).ToArray();
                    if(remainingWindows.Length > 0)
                    {
                        var message = "The GuiManager has " + remainingWindows.Length +
                            " windows.\n";
                        message += $"The first is of type {remainingWindows[0].GetType()} named {remainingWindows[0].Name}\n";
                        message += "See \"FlatRedBall.Gui.GuiManager.Windows\" or add the window to PersistentWindows if it should persist between screens";

                        messages.Add(message);
                    }
                }

                #endregion

                if (messages.Count != 0)
                {
                    string errorString = "The Screen that was just unloaded did not clean up after itself:";
                    if(mCurrentScreen != null)
                    {
                        errorString = $"The Screen that was just unloaded ({mCurrentScreen.GetType().Name}) did not clean up after itself:";
                    }
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

