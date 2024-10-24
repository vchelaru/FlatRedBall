#if ANDROID || IOS || DESKTOP_GL
#define REQUIRES_PRIMARY_THREAD_LOADING
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using FlatRedBall.Math;
using FlatRedBall.Gui;
using FlatRedBall.Instructions;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics;
using FlatRedBall.Utilities;
using System.Threading;
using FlatRedBall.Input;
using FlatRedBall.IO;
using System.Collections;
using System.Linq;
using System.Diagnostics;

namespace FlatRedBall.Screens
{
    #region Enums

    public enum AsyncLoadingState
    {
        NotStarted,
        LoadingScreen,
        Done
    }

    #endregion

    /// <summary>
    /// Base class for screens typically defined through Glue.
    /// </summary>
    public class Screen : INameable, IInstructable
    {
        #region Fields

        /// <summary>
        /// Cancellation token source which can be used by any async call to cancel whenever the screen is destroyed.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; private set; } = new CancellationTokenSource();

        int mNumberOfThreadsBeforeAsync = -1;

        public bool IsPaused { get; private set; } = false;

        /// <summary>
        /// The time using FlatRedBall.CurrentTime (time since the app started running) which is used
        /// to determine the screen time.
        /// </summary>
        /// <remarks>
        /// This value starts at 0 when a Screen is first created. This can be modified to restart screen timer, but
        /// it can cause unexpected side effects for systems which depend on timing, so this should not be changed in normal gameplay.
        /// </remarks>
        protected double mTimeScreenWasCreated;
        protected double mAccumulatedPausedTime = 0;

        protected Layer mLayer;

        private string mContentManagerName;

        protected Scene mLastLoadedScene;
        private bool mIsActivityFinished;
        private string mNextScreen;

        private bool mManageSpriteGrids;

        internal Screen mNextScreenToLoadAsync;

        /// <summary>
        /// Stores the names and values of variables which should be preserved on the next
        /// restart. These values are recorded prior to the screen being destroyed, then applied
        /// after the construction of the next screen. These values are used internally in the base
        /// screen class and should not be modified.
        /// </summary>
        static Dictionary<string, object> RestartVariableValues { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// A collection of variables to reset. This can be variables on the Screen itself or variables on objects within
        /// the screen, like this.PlayerInstance.X. Note that any objects which belong to the screen must begin with the name "this".
        /// </summary>
        protected static List<string> RestartVariables { get; private set; } = new List<string>();

        
        static List<Type> cachedDerivedScreenTypes;

        #endregion

        #region Properties

        /// <summary>
        /// The list of instructions owned 
        /// by this screen.
        /// </summary>
        /// <remarks>
        /// These instructions will be automatically
        /// executed based off of time.  Execution of
        /// these instructions is automatically handled 
        /// by the InstructionManager.
        /// </remarks>
        public InstructionList Instructions
        {
            get;
            private set;
        }

        /// <summary>
        /// [Deprecated] Whether the Screen's Layer should be removed when the Screen is destroyed.
        /// This will be removed in future versions of FlatRedBall.
        /// </summary>
        [Obsolete("Marked Deprecated on April 6, 2024. This property will be removed in future versions of FlatRedBall")]
        public bool ShouldRemoveLayer
        {
            get;
            set;
        }

        /// <summary>
        /// Returns how much time the Screen has spent paused
        /// </summary>
        public double AccumulatedPauseTime
        {
            get
            {
                // Internally we just use the accumulated pause time
                // without considering when the Screen was started.  But
                // for external reporting we need to report just the actual
                // paused time.
                return mAccumulatedPausedTime - mTimeScreenWasCreated;
            }
        }

        /// <summary>
        /// Returns the number of seconds since the Screen was initialized, excluding paused time.
        /// This value begins at 0 when the Screen is first created and is reset when the screen restarts.
        /// </summary>
        /// <remarks>
        /// If a screen is never paused, then its PauseAdjustedCurrentTime value increases regularly.
        /// If a screen is never paused and it is the first screen in the game, then PauseAdjustedCurrentTime
        /// will equal TimeManager.CurrentTime.
        /// </remarks>
        /// <seealso cref="AccumulatedPauseTime"/>
        public double PauseAdjustedCurrentTime
        {
            get { return TimeManager.CurrentTime - mAccumulatedPausedTime; }
        }

        /// <summary>
        /// Action raised when this screen is destroyed. This can be used to create flow objects or for top-level
        /// debugging/game editors.
        /// </summary>
        public Action ScreenDestroy { get; set; }

        public int ActivityCallCount
        {
            get;
            set;
        }

        public string ContentManagerName
        {
            get { return mContentManagerName; }
        }

        /// <summary>
        /// Gets and sets whether the activity is finished for a particular screen. Setting this to 
        /// true will notify the Screen that it should destroy itself and move on to the next screen.
        /// </summary>
        /// <remarks>
        /// If activity is finished, then the ScreenManager or parent
        /// screen (if the screen is a popup) knows to destroy the screen
        /// and loads the NextScreen class.</remarks>
        /// <seealso cref="MoveToScreen(string)"/>
        /// <seealso cref="MoveToScreen(Type)"/>
        public bool IsActivityFinished
        {
            get { return mIsActivityFinished; }
            set 
			{
                mIsActivityFinished = value; 
			}

        }

        public AsyncLoadingState AsyncLoadingState
        {
            get;
            private set;
        }

        [Obsolete("Use DefaultLayer instead of Layer")]
        public Layer Layer
        {
            get => DefaultLayer; 
            set => DefaultLayer = value; 
        }

        public Layer DefaultLayer
        {
            get => mLayer;
            set => mLayer = value;
        }

        public bool ManageSpriteGrids
        {
            get { return mManageSpriteGrids; }
            set { mManageSpriteGrids = value; }
        }

        /// <summary>
        /// The name of the Screen-inheriting to load next. This can be fully qualified "Namespace.Screens.ScreenName" or 
        /// just the screen name. If just a screen name is specified then current screen's namespace will be prepended.
        /// to link to.
        /// </summary>
        /// <remarks>
        /// This property is read by the ScreenManager when IsActivityFinished is
        /// set to true.  Therefore, this must always be set to some value before
        /// or in the same frame as when IsActivityFinished is set to true.
        /// </remarks>
        public string NextScreen
        {
            get { return mNextScreen; }
            set { mNextScreen = value; }
        }

        protected bool UnloadsContentManagerWhenDestroyed
        {
            get;
            set;
        }

        public bool HasDrawBeenCalled
        {
            get;
            internal set;
        }

        public string Name { get; set; }

        #endregion

        #region Events/Delegates

        Action ActivatingAction;
        Action DeactivatingAction;

        /// <summary>
        /// Event raised before CustomInitialize is called.
        /// </summary>
        public Action BeforeCustomInitialize;

        #endregion

        #region Methods

        #region Constructor

        /// <summary>
        /// Creates a new Screen using the Global content manager
        /// </summary>
        public Screen()
        {
            DoConstructorLogic(FlatRedBallServices.GlobalContentManager);
        }

        /// <summary>
        /// Creates a new Screen using the specified content manager name
        /// </summary>
        /// <param name="contentManagerName">The content manager name to use when loading content for this screen.</param>
        public Screen(string contentManagerName)
        {
            // June 13, 2024
            // Added this constructor
            // to allow code-only projects
            // to instantiate using global if
            // they don't care about which content
            // manager to use.
            DoConstructorLogic(contentManagerName);
        }

        private void DoConstructorLogic(string contentManagerName)
        {
            this.Instructions = new InstructionList();
            ShouldRemoveLayer = true;
            UnloadsContentManagerWhenDestroyed = true;
            mContentManagerName = contentManagerName;
            mManageSpriteGrids = true;
            IsActivityFinished = false;

            mLayer = ScreenManager.NextScreenLayer;

            ActivatingAction = new Action(Activating);
            DeactivatingAction = new Action(OnDeactivating);

            if (ScreenManager.ShouldActivateScreen)
            {
                Activating();
            }
        }

        #endregion

        #region Public Methods
        public virtual void Activating()
        {
            this.PreActivate();// for generated code to override, to reload the statestack
            this.OnActivate();// for user created code
        }

        private void OnDeactivating()
        {
            this.PreDeactivate();// for generated code to override, to save the statestack
            this.OnDeactivate();// for user generated code;
        }

        #region Activation Methods

        protected virtual void OnActivate()
        {
        }

        protected virtual void PreActivate()
        {
        }

        protected virtual void OnDeactivate()
        {
        }

        protected virtual void PreDeactivate()
        {
        }

        #endregion

        public virtual void Activity(bool firstTimeCalled)
        {
            if (IsPaused)
            {
                mAccumulatedPausedTime += TimeManager.SecondDifference;
            }
        }

        public virtual void ActivityEditMode()
        {

        }

        Type asyncScreenTypeToLoad = null;

		public void StartAsyncLoad(Type type, Action afterLoaded = null)
        {
            StartAsyncLoad(type.FullName, afterLoaded);
        }

        public void StartAsyncLoad(string screenType, Action afterLoaded = null)
        {
            if (AsyncLoadingState == AsyncLoadingState.LoadingScreen)
            {
#if DEBUG
                throw new InvalidOperationException("This Screen is already loading a Screen of type " + asyncScreenTypeToLoad + ".  This is a DEBUG-only exception");
#endif
            }
            else if (AsyncLoadingState == AsyncLoadingState.Done)
            {
#if DEBUG
                throw new InvalidOperationException("This Screen has already loaded a Screen of type " + asyncScreenTypeToLoad + ".  This is a DEBUG-only exception");
#endif
            }
            else
            {
				AsyncLoadingState = AsyncLoadingState.LoadingScreen;
				asyncScreenTypeToLoad = ScreenManager.MainAssembly.GetType(screenType);

				if (asyncScreenTypeToLoad == null)
				{
					throw new Exception("Could not find the type " + screenType);
				}

				#if REQUIRES_PRIMARY_THREAD_LOADING
				// We're going to do the "async" loading on the primary thread
				// since we can't actually do it async.
				PerformAsyncLoad();
                if(afterLoaded != null)
                {
                    afterLoaded();
                }
				#else

                mNumberOfThreadsBeforeAsync = FlatRedBallServices.GetNumberOfThreadsToUse();

                FlatRedBallServices.SetNumberOfThreadsToUse(1);

                Action action;

                if(afterLoaded == null)
                {
                    action = (Action)PerformAsyncLoad;
                }
                else
                {
                    action = () =>
                        {
                            PerformAsyncLoad();

                            // We're going to add this to the instruction manager so it executes on the main thread:
                            InstructionManager.AddSafe(new DelegateInstruction(afterLoaded));
                        };
                }



                ThreadStart threadStart = new ThreadStart(action);
                Thread thread = new Thread(threadStart);
                thread.Start();

				#endif
            }
        }

        private void PerformAsyncLoad()
        {
            mNextScreenToLoadAsync = (Screen)Activator.CreateInstance(asyncScreenTypeToLoad, new object[0]);

            // Don't add it to the manager!
            mNextScreenToLoadAsync.Initialize(addToManagers:false);

            AsyncLoadingState = AsyncLoadingState.Done;
        }

        public virtual void Initialize(bool addToManagers)
        {

        }

        public virtual void AddToManagers()
        {	
			// We want to start the timer when we actually add to managers - this is when the activity for the Screen starts
			mAccumulatedPausedTime = TimeManager.CurrentTime;
            mTimeScreenWasCreated = TimeManager.CurrentTime;
        }

        /// <summary>
        /// Performs the unloading of content and destroys all contained objects. 
        /// Typically this method should not be called directly, but instead the IsActivityFinished 
        /// property is set to true, which internally calls Destroy.
        /// </summary>
        public virtual void Destroy()
        {

            if (mLastLoadedScene != null)
            {
                mLastLoadedScene.Clear();
            }


            FlatRedBall.Debugging.Debugger.DestroyText();

            // It's common for users to forget to add Particle Sprites
            // to the mSprites SpriteList.  This will either create leftover
            // particles when the next screen loads or will throw an assert when
            // the ScreenManager checks if there are any leftover Sprites.  To make
            // things easier we'll just clear the Particle Sprites here.
            bool isPopup = this != ScreenManager.CurrentScreen;
            if (!isPopup)
                SpriteManager.RemoveAllParticleSprites();

            if (UnloadsContentManagerWhenDestroyed && mContentManagerName != FlatRedBallServices.GlobalContentManager)
            {
                FlatRedBallServices.Unload(mContentManagerName);
                FlatRedBallServices.Clean();
            }

            if (ShouldRemoveLayer && mLayer != null)
            {
                SpriteManager.RemoveLayer(mLayer);
            }
            if (IsPaused)
            {
                UnpauseThisScreen();
            }
			
			GuiManager.Cursor.IgnoreInputThisFrame = true;

            if(mNumberOfThreadsBeforeAsync != -1)
            {
                FlatRedBallServices.SetNumberOfThreadsToUse(mNumberOfThreadsBeforeAsync);
            }

            if (ScreenDestroy != null)
                ScreenDestroy();
        }

        /// <summary>
        /// Pauses all Instructable objects, disables collision, and sets IsPaused to true.
        /// </summary>
        public virtual void PauseThisScreen()
        {
            this.IsPaused = true;
            InstructionManager.PauseEngine();
            Math.Collision.CollisionManager.Self.IsPausedByScreen = true;

        }

        /// <summary>
        /// Executes all Unpause instructions, enables collision, and sets the IsPaused property to false.
        /// </summary>
        public virtual void UnpauseThisScreen()
        {
            InstructionManager.UnpauseEngine();
            Math.Collision.CollisionManager.Self.IsPausedByScreen = false;
            this.IsPaused = false;
        }

        /// <summary>
        /// Returns the number of seconds since the argument time for the current screen, considering pausing.
        /// </summary>
        /// <remarks>
        /// Each screen has an internal timer which keeps track of how much time has passed since it has been constructed.
        /// This timer always begins at 0. Therefore, the following code will always tell you how long the screen has been alive:
        /// var timeScreenHasBeenAlive = ScreenInstance.PauseAdjustedSecondsSince(0);
        /// </remarks>
        /// <example>
        /// PauseAdjustedSecondsSince can be used to determine if some timed event has expired. For example:
        /// bool isCooldownFinished = PauseAdjustedSecondsSince(lastAbilityUse) >= CooldownTime;
        /// </example>
        /// <param name="time">The time from which to check how much time has passed.</param>
        /// <returns>How much time has passed since the parameter value.</returns>
        public double PauseAdjustedSecondsSince(double time)
        {
            return PauseAdjustedCurrentTime - time;
        }

        /// <summary>
        /// Destroys and re-creates this screen.
        /// </summary>
        /// <remarks>
        /// This can be used to begin a level from the beginning or to reload a screen for debugging.
        /// </remarks>
        /// <param name="reloadScreenContent">Whether the screen's content should be reloaded. 
        /// If true, then any content that belongs
        /// to this Screen's content manager will be reloaded. 
        /// Global content will not be reloaded.</param>
        /// <param name="applyRestartVariables">Whether to apply restart variables. If true, then any restart variables
        /// will be applied, which is useful when iterating on a game. This should be false if restarting
        /// due to gameplay events such as a player dying.</param>
        public void RestartScreen(bool reloadScreenContent = true, bool applyRestartVariables = true)
        {
            if (reloadScreenContent == false)
            {
                UnloadsContentManagerWhenDestroyed = false;
            }
            if(applyRestartVariables)
            {
                StoreRestartVariableValues();
            }
            else
            {
                RestartVariableValues.Clear();
                RestartVariables.Clear();
            }

            MoveToScreen(this.GetType());
        }

        private void StoreRestartVariableValues()
        {
            RestartVariableValues.Clear();

            foreach (var variableName in RestartVariables)
            {
                var value = GetValueForVariableName(variableName);
                RestartVariableValues.Add(variableName, value);
            }

            RestartVariables.Clear();
        }

        internal void ApplyRestartVariables()
        {
            foreach(var kvp in RestartVariableValues)
            {
                ApplyVariable(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Returns the instance of the object represented by the variable name. This must begin with "this.". For example, 
        /// passing "this.PlayerInstance" will return the PlayerInstance object.
        /// </summary>
        /// <param name="variableName"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public object GetInstanceRecursive(string variableName, object container = null)
        {
            if(variableName.Contains("."))
            {
                GetInstance(variableName, container, out string afterDot, out object instance);

                return GetInstanceRecursive(afterDot, instance);
            }
            return container;

        }

        /// <summary>
        /// Applies a value to a variable like "this.Player.X". 
        /// </summary>
        /// <remarks>
        /// This can be used by scripting systems, but is also used internally by
        /// screens when restarting.
        /// </remarks>
        /// <param name="variableName">The variable name, where the name must start with "this." if it is on an instance on the screen.</param>
        /// <param name="value">The value, as an object.</param>
        /// <param name="container">The parent object, allowing variables like this.Object.Subobject, where Object is the parent.</param>
        public bool ApplyVariable(string variableName, object value, object container = null)
        {
            var wasSet = false;
            if (variableName.Contains("."))
            {
                string afterDot;
                object instance;
                GetInstance(variableName, container, out afterDot, out instance);

                if (instance is IList asIList)
                {
                    for(int i = 0; i < asIList.Count; i++)
                    {
                        var instanceInList = asIList[i];

                        var effectiveValue = value;

                        if(value is IList valueList)
                        {
                            if(i < valueList.Count)
                            {
                                effectiveValue = valueList[i];
                            }
                        }

                        ApplyVariable(afterDot, effectiveValue, instanceInList);
                    }
                }
                else if(instance != null) // the instance may be null if the chain of assignments doesn't have a value
                {
                    wasSet = ApplyVariable(afterDot, value, instance);
                }
            }
            else
            {
                if (container is IList asIList)
                {
                    foreach (var item in asIList)
                    {
                        FlatRedBall.Instructions.Reflection.LateBinder.SetValueStatic(item, variableName, value);
                        wasSet = true;

                    }
                }
                else if(container is Type asType)
                {
                    var field = asType.GetField(variableName, BindingFlags.Static | BindingFlags.Public);
                    if(field != null)
                    {
                        field.SetValue(value, null);
                        wasSet = true;
                    }
                }
                else if(container != null)
                {
                    wasSet = FlatRedBall.Instructions.Reflection.LateBinder.SetValueStatic(container, variableName, value);
                }
            }

            return wasSet;
        }

        public object GetInstance(string instanceName, object container = null)
        {
            GetInstance(instanceName, container, out string _, out object instance);
            return instance;
        }

        /// <summary>
        /// Obtains the instance object represented after the dot using reflection. The variable
        /// should begin with "this" if it is an object on the screen. For example, passing "this.SpriteList"
        /// will return the SpriteList object.
        /// </summary>
        /// <param name="instanceName">The variable name with dots such as "this.SpriteList"</param>
        /// <param name="container">The container of the object. Should be null for objects contained in the screen.</param>
        /// <param name="afterDot">The variable after the instance has been returned, allowing for recursive calls. For example, passing Sprite.X will return X</param>
        /// <param name="instance">The instance on the variable before the first dot. </param>
        public void GetInstance(string instanceName, object container, out string afterDot, out object instance)
        {
            var indexOfDot = instanceName.IndexOf(".");

            string beforeDot = instanceName;
            afterDot = string.Empty;
            if (indexOfDot != -1)
            {
                beforeDot = instanceName.Substring(0, indexOfDot);
                afterDot = instanceName.Substring(indexOfDot + 1);
            }
            instance = null;
            if (container == null && beforeDot == "this")
            {
                instance = this;
            }
            else if(container == null)
            {
                // Doesnt start with "this" so it's a static variable
                var ownerType = ScreenManager.MainAssembly.GetTypes()
                    .Where(item => instanceName.StartsWith(item.FullName))
                    // longest means it's the most derived.
                    .OrderBy(item => item.Name.Length)
                    .LastOrDefault();

                if(ownerType != null)
                {
                    afterDot = instanceName.Substring((ownerType.FullName + ".").Length);
                    instance = ownerType;
                }
            }
            else if(container == null && beforeDot == "Camera")
            {
                instance = typeof(Camera);
            }
            else if (container != null)
            {
                if(container is Type containerType && containerType == typeof(Camera))
                {
                    if(beforeDot == "Main")
                    {
                        instance = Camera.Main;
                    }
                }
                else if (beforeDot.Contains("["))
                {
                    var openBracketIndex = beforeDot.IndexOf("[");
                    var closeBracketInex = beforeDot.IndexOf("]");
                    var startOfInt = openBracketIndex + 1;
                    var length = closeBracketInex - startOfInt;

                    var asString = beforeDot.Substring(startOfInt, length);

                    var index = int.Parse(asString);

                    string memberName = beforeDot.Substring(0, openBracketIndex);

                    instance = FlatRedBall.Instructions.Reflection.LateBinder.GetValueStatic(container, memberName);

                    var listType = instance.GetType();
                    var method = listType.GetMethod("get_Item");

                    instance = method.Invoke(instance, new object[] { index });

                }
                else
                {
                    try
                    {
                        // In case there is no object by that name, don't crash. We need to suppress errors 
                        // because Glue commands will blindly send update commands on properties on objects that
                        // may be a base or derived type, and if the entity is of the base type, it may really not
                        // have a property.
                        instance = FlatRedBall.Instructions.Reflection.LateBinder.GetValueStatic(container, beforeDot);
                    }
                    catch
                    {
                        instance = null;
                    }

                    // names can change and not match the original instance name, so let's verify that.
                    // Update August 14, 2021 - objects from Glue may not have their names assigned. For
                    // example, LayeredTileMaps do not currently have their name assigned. They could, but
                    // then the question is - should the name match the Object in Glue? Or the file name? There's
                    // arguments for both. Vic didn't want to pick one right now because he wasn't sure what the preferred
                    // approach is. For example, Texture2D's use their file name as their name - should TMX files too? Hard
                    // to say. However, if it's nameable and the name doesn't match, let's null it out only if the object does
                    // have a name. If the name is null, then that means the object didn't change its name, but rather it never 
                    // had one.
                    // Update May 6, 2023
                    // The plot thickens! Unfortunately TileShapeCollections which use Tile shapes pull directly from the map's internal
                    // TileShapeCollections, where each TileShapeCollection has its name matching the Type that is put in it. These names may
                    // not match up. Ultimately this should be enforced by FRB as convention, and we should have errors for this. Vic is going
                    // to add a card to make sure this is enforced in Glue.
                    if(instance is INameable nameable && nameable.Name != beforeDot && !string.IsNullOrEmpty(nameable.Name))
                    {
                        instance = null;
                    }

                    if(instance == null && container is Screen)
                    {
                        instance = SpriteManager.ManagedPositionedObjects.FirstOrDefault(item => item.Name == beforeDot);
                    }
                    // This could be an object that was added to an entity dynamically, so it won't be found
                    // through reflection but it may be found through the parent/child relationship
                    if(instance == null && container is PositionedObject containerAsPositionedObject)
                    {
                        instance = containerAsPositionedObject.Children.FirstOrDefault(item => item.Name == beforeDot);
                    }
                }
            }
            else
            {
                throw new Exception("Variable must start with \"this\"");
            }
        }

        private object GetValueForVariableName(string variableName, object container = null)
        {
            if (variableName.Contains("."))
            {
                string afterDot;
                object instance;
                GetInstance(variableName, container, out afterDot, out instance);

                if (instance is IList asList)
                {
                    List<object> toReturn = new List<object>();

                    foreach (var item in asList)
                    {
                        var itemValue = GetValueForVariableName(afterDot, item);
                        toReturn.Add(itemValue);
                    }
                    return toReturn;
                }
                else
                {
                    return GetValueForVariableName(afterDot, instance);
                }
            }
            else
            {
                return FlatRedBall.Instructions.Reflection.LateBinder.GetValueStatic(container, variableName);
            }
        }


        #endregion

        #region Screen Navigation Methods

        /// <summary>Tells the screen that it should destroy itself
        /// and move to the screen with the matching (fully qualified) name.
        /// This method is usually used to move between screens or levels.
        /// </summary>
        /// <param>Fully Qualified Type of the screen to move to. If this value is null or empty then an exception is thrown.</param>
        public void MoveToScreen(string screenClass)
        {
            if(string.IsNullOrEmpty(screenClass))
            {
                throw new ArgumentException("screenClass must not be null or empty");
            }
            IsActivityFinished = true;
            NextScreen = screenClass;
        }
		
        public void MoveToScreen(Type screenType)
        {
            IsActivityFinished = true;
            NextScreen = screenType.FullName;
        }
        
        #endregion

        #region Protected Methods

        /// <param name="state">This should be a valid enum value of the concrete screen type.</param>
        [Obsolete("This is no longer used for anything as of March 27 2022, and will be removed in a future version", error:true)]
        public virtual void MoveToState(int state)
        {
            // no-op
        }

        /// <summary>Default implementation tells the screen manager to finish this screen's activity and navigate
        /// to the previous screen on the backstack.</summary>
        /// <remarks>Override this method if you want to have custom behavior when the back button is pressed.</remarks>
        protected virtual void HandleBackNavigation()
        {

        }

        public virtual void UpdateDependencies(double currentTime)
        {

        }

        #endregion

        #endregion
    }
}
